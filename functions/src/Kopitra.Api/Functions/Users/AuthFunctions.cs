using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Application.Users.Services;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.ValueObjects;
using Kopitra.Api.Functions.Users.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Security.Claims;

namespace Kopitra.Api.Functions.Users;

/// <summary>
/// HTTP function endpoints for authentication
/// Handles user registration, login, token refreshToken, and logout
/// </summary>
public class AuthFunctions
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryProcessor _queryProcessor;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthFunctions(
        ICommandBus commandBus,
        IQueryProcessor queryProcessor,
        IClock clock,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _commandBus = commandBus;
        _queryProcessor = queryProcessor;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// POST /api/auth/register - User registration
    /// </summary>
    [Function("Register")]
    [OpenApiOperation(operationId: "Register", tags: new[] { "Auth" }, Summary = "Register a new user")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RegisterUserRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(RegisterResponse), Description = "User created")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ApiResponse), Description = "Bad request")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsJsonAsync<RegisterUserRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(body.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Email is required"));
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(body.Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Password is required"));
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(body.DisplayName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Display name is required"));
                return badResponse;
            }

            // Hash password
            var passwordHash = _passwordHasher.Hash(body.Password);

            // Create new user
            var userId = UserId.New;
            var command = new RegisterUserCommand(userId)
            {
                Email = body.Email,
                DisplayName = body.DisplayName,
                PasswordHash = passwordHash
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Location", $"/api/users/{userId.Value}");
            await response.WriteAsJsonAsync(new RegisterResponse
            {
                UserId = userId.Value,
                Email = body.Email,
                DisplayName = body.DisplayName
            });
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/auth/login - User login
    /// </summary>
    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Auth" }, Summary = "Authenticate user and return tokens")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse), Description = "Authentication successful")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsJsonAsync<LoginRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(body.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Email is required"));
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(body.Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Password is required"));
                return badResponse;
            }

            var user = await _queryProcessor.ProcessAsync(new GetUserByEmailQuery(body.Email), CancellationToken.None);
            if (user == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse("Invalid email or password"));
                return unauthorizedResponse;
            }

            var isPasswordValid = _passwordHasher.Verify(body.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse("Invalid email or password"));
                return unauthorizedResponse;
            }

            if (!user.IsActive)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse("User account is inactive"));
                return unauthorizedResponse;
            }

            var accessToken = _tokenService.GenerateToken(user.Id, new[] { new Claim("email", user.Email) });
            var refreshToken = Guid.NewGuid().ToString("N");
            var expires = _clock.UtcNow.AddDays(30);

            // Persist refreshToken token via aggregate command
            var userId = new UserId(user.Id);
            var issueCmd = new IssueRefreshTokenCommand(userId)
            {
                RefreshToken = refreshToken,
                ExpiresAt = expires.DateTime,
            };
            await _commandBus.PublishAsync(issueCmd, CancellationToken.None).ConfigureAwait(false);

            // Record login (audit)
            var recordCmd = new RecordUserLoginCommand(userId);
            await _commandBus.PublishAsync(recordCmd, CancellationToken.None).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600
            });
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/auth/refreshToken - Token refreshToken
    /// </summary>
    [Function("RefreshToken")]
    [OpenApiOperation(operationId: "RefreshToken", tags: new[] { "Auth" }, Summary = "Refresh access token using refresh token")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LoginResponse), Description = "New tokens")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    public async Task<HttpResponseData> RefreshToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsJsonAsync<RefreshTokenRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Refresh token is required"));
                return badResponse;
            }

            var user = await _queryProcessor.ProcessAsync(new GetUserByRefreshTokenQuery(body.RefreshToken), CancellationToken.None);
            if (user == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse("Invalid refresh token"));
                return unauthorizedResponse;
            }

            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < _clock.UtcNow)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse("Refresh token expired"));
                return unauthorizedResponse;
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateToken(user.Id, new[] { new Claim("email", user.Email) });
            var newRefreshToken = Guid.NewGuid().ToString("N");
            var expires = _clock.UtcNow.AddDays(30);

            // Persist new newRefreshToken token via aggregate command
            var userId = new UserId(user.Id);
            var issueCmd = new IssueRefreshTokenCommand(userId)
            {
                RefreshToken = newRefreshToken,
                ExpiresAt = expires.DateTime,
            };
            await _commandBus.PublishAsync(issueCmd, CancellationToken.None).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 3600
            });
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return unauthorizedResponse;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/auth/logout - User logout
    /// </summary>
    [Function("Logout")]
    [OpenApiOperation(operationId: "Logout", tags: new[] { "Auth" }, Summary = "Logout current user")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ApiResponse), Description = "Logout successful")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
    {
        try
        {
            // Extract user ID from JWT token
            var userId = req.ExtractUserIdFromToken();
            if (userId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            // In production, invalidate refreshToken token in database
            // For now, just return success
            await Task.CompletedTask;

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse("Logged out successfully"));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
    }
}
