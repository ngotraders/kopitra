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

namespace Kopitra.Api.Functions.Users;

/// <summary>
/// HTTP function endpoints for user management
/// Handles authentication, user registration, profile updates, and permission management
/// </summary>
public class UsersFunctions
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryProcessor _queryProcessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly IPasswordHasher _passwordHasher;

    public UsersFunctions(
    ICommandBus commandBus,
    IQueryProcessor queryProcessor,
    IAuthorizationService authorizationService,
    IPasswordHasher passwordHasher)
    {
        _commandBus = commandBus;
        _queryProcessor = queryProcessor;
        _authorizationService = authorizationService;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// GET /api/users/me - Get current logged-in user information
    /// </summary>
    [Function("GetCurrentUser")]
    [OpenApiOperation("GetCurrentUser", new[] { "Users" }, Summary = "Get current user")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiResponse), Description = "Not found")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UserResponse))]
    public async Task<HttpResponseData> GetCurrentUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            var userId = req.ExtractUserIdFromToken();
            if (userId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var query = new GetUserByIdQuery(userId);
            var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            if (!user.IsActive)
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var userResponse = new UserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CanProvide = user.CanProvide,
                CanSubscribe = user.CanSubscribe,
                IsActive = user.IsActive,
                RegisteredAt = user.RegisteredAt,
                LastLoginAt = user.LastLoginAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(userResponse);
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
    /// GET /api/users - Get all users (admin only)
    /// </summary>
    [Function("GetAllUsers")]
    [OpenApiOperation("GetAllUsers", new[] { "Users" }, Summary = "Get all users (admin only)")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<UserResponse>))]
    public async Task<HttpResponseData> GetAllUsers(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req)
    {
        try
        {
            var requestingUserId = req.ExtractUserIdFromToken();
            if (requestingUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            if (!_authorizationService.IsAdmin(requestingUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var query = new GetAllUsersQuery();
            var users = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            var usersResponse = users.Select(u => new UserResponse
            {
                UserId = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CanProvide = u.CanProvide,
                CanSubscribe = u.CanSubscribe,
                IsActive = u.IsActive,
                RegisteredAt = u.RegisteredAt,
                LastLoginAt = u.LastLoginAt
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(usersResponse);
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
    /// GET /api/users/{userId} - Get user details (self or admin)
    /// </summary>
    [Function("GetUserById")]
    [OpenApiOperation("GetUserById", new[] { "Users" }, Summary = "Get user by id")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiResponse), Description = "Not found")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UserResponse))]
    public async Task<HttpResponseData> GetUserById(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}")] HttpRequestData req,
    string userId)
    {
        try
        {
            var requestingUserId = req.ExtractUserIdFromToken();
            if (requestingUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var targetUserId = new UserId(userId);
            if (!_authorizationService.CanViewUser(requestingUserId, targetUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var query = new GetUserByIdQuery(targetUserId);
            var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            var userResponse = new UserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CanProvide = user.CanProvide,
                CanSubscribe = user.CanSubscribe,
                IsActive = user.IsActive,
                RegisteredAt = user.RegisteredAt,
                LastLoginAt = user.LastLoginAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(userResponse);
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
    /// PUT /api/users/{userId} - Update user information (self or admin)
    /// </summary>
    [Function("UpdateUser")]
    [OpenApiOperation("UpdateUser", new[] { "Users" }, Summary = "Update user info")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(UserUpdateRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse))]
    public async Task<HttpResponseData> UpdateUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{userId}")] HttpRequestData req,
    string userId)
    {
        try
        {
            var requestingUserId = req.ExtractUserIdFromToken();
            if (requestingUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var targetUserId = new UserId(userId);
            if (!_authorizationService.CanManageUser(requestingUserId, targetUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var body = await req.ReadAsJsonAsync<UserUpdateRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            var command = new UpdateUserInfoCommand(targetUserId)
            {
                Email = body.Email,
                DisplayName = body.DisplayName
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse("User updated successfully"));
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
    /// POST /api/users - Create user (admin only)
    /// </summary>
    [Function("CreateUser")]
    [OpenApiOperation("CreateUser", new[] { "Users" }, Summary = "Create user (admin only)")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(RegisterUserRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(RegisterResponse))]
    public async Task<HttpResponseData> CreateUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
    {
        try
        {
            var requestingUserId = req.ExtractUserIdFromToken();
            if (requestingUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            if (!_authorizationService.IsAdmin(requestingUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var body = await req.ReadAsJsonAsync<RegisterUserRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            var userId = UserId.New;
            var password = string.IsNullOrWhiteSpace(body.Password) ? Guid.NewGuid().ToString("N") : body.Password;
            var passwordHash = _passwordHasher.Hash(password);

            var command = new RegisterUserCommand(userId)
            {
                Email = body.Email!,
                DisplayName = body.DisplayName!,
                PasswordHash = passwordHash
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Location", $"/api/users/{userId.Value}");
            await response.WriteAsJsonAsync(new RegisterResponse { UserId = userId.Value, Email = body.Email, DisplayName = body.DisplayName });
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
    /// PUT /api/users/{userId}/permissions - Change user permissions (admin only)
    /// </summary>
    [Function("ChangePermissions")]
    [OpenApiOperation("ChangePermissions", new[] { "Users" }, Summary = "Change user permissions (admin only)")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(ChangePermissionsRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse))]
    public async Task<HttpResponseData> ChangePermissions(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{userId}/permissions")] HttpRequestData req,
    string userId)
    {
        try
        {
            var adminUserId = req.ExtractUserIdFromToken();
            if (adminUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            if (!_authorizationService.IsAdmin(adminUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var body = await req.ReadAsJsonAsync<ChangePermissionsRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            var targetUserId = new UserId(userId);
            var command = new ChangeUserPermissionsCommand(targetUserId)
            {
                CanProvide = body.CanProvide,
                CanSubscribe = body.CanSubscribe
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse("Permissions updated successfully"));
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
    /// PUT /api/users/{userId}/status - Enable/disable user (admin only)
    /// </summary>
    [Function("SetStatus")]
    [OpenApiOperation("SetStatus", new[] { "Users" }, Summary = "Enable or disable user (admin only)")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(StatusChangeRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse))]
    public async Task<HttpResponseData> SetStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{userId}/status")] HttpRequestData req,
    string userId)
    {
        try
        {
            var adminUserId = req.ExtractUserIdFromToken();
            if (adminUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            if (!_authorizationService.IsAdmin(adminUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var body = await req.ReadAsJsonAsync<StatusChangeRequest>();
            if (body == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Request body required"));
                return badResponse;
            }

            var targetUserId = new UserId(userId);

            if (body.IsActive)
            {
                var cmd = new ReactivateUserCommand(targetUserId)
                {
                    AdminUserId = adminUserId,
                    Memo = body.Memo
                };
                await _commandBus.PublishAsync(cmd, CancellationToken.None);
            }
            else
            {
                var cmd = new DeactivateUserCommand(targetUserId)
                {
                    AdminUserId = adminUserId,
                    Reason = body.Reason ?? "No reason provided"
                };
                await _commandBus.PublishAsync(cmd, CancellationToken.None);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse("User status updated"));
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
    /// DELETE /api/users/{userId} - Delete user (admin only)
    /// </summary>
    [Function("DeleteUser")]
    [OpenApiOperation("DeleteUser", new[] { "Users" }, Summary = "Delete user (admin only)")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse))]
    public async Task<HttpResponseData> DeleteUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{userId}")] HttpRequestData req,
    string userId)
    {
        try
        {
            var adminUserId = req.ExtractUserIdFromToken();
            if (adminUserId == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            if (!_authorizationService.IsAdmin(adminUserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var targetUserId = new UserId(userId);

            var command = new DeactivateUserCommand(targetUserId)
            {
                AdminUserId = adminUserId,
                Reason = "Deleted by admin"
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse("User deleted"));
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
