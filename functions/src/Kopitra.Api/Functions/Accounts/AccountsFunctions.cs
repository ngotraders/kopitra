using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Application.Users.Services;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.ValueObjects;
using Kopitra.Api.Functions.Accounts.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;

namespace Kopitra.Api.Functions.Accounts;

/// <summary>
/// HTTP function endpoints for account management
/// Handles account registration, activation, and verification
/// </summary>
public class AccountsFunctions
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryProcessor _queryProcessor;
    private readonly IHttpRequestDataAccessor _requestDataAccessor;
    private readonly IAuthorizationService _authorizationService;

    public AccountsFunctions(
        ICommandBus commandBus,
        IQueryProcessor queryProcessor,
        IHttpRequestDataAccessor requestDataAccessor,
        IAuthorizationService authorizationService)
    {
        _commandBus = commandBus;
        _queryProcessor = queryProcessor;
        _requestDataAccessor = requestDataAccessor;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// POST /api/accounts - Register a new account (Flow 1)
    /// </summary>
    [Function("RegisterAccount")]
    [OpenApiOperation("RegisterAccount", new[] { "Accounts" }, Summary = "Register a new account")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(RegisterAccountRequest), Description = "Account registration request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(AccountResponse))]
    public async Task<HttpResponseData> RegisterAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts")] HttpRequestData req)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var requestBody = await req.ReadAsJsonAsync<RegisterAccountRequest>();
            if (requestBody == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Invalid request body"));
                return badResponse;
            }

            // Check authorization: user can register their own or admin can register for others
            if (requestBody.UserId != tokenValues.UserId && !await _authorizationService.IsAdminAsync(tokenValues.UserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var accountId = new AccountId($"account-{Guid.NewGuid()}");
            var userId = new UserId(requestBody.UserId);
            var brokerType = Enum.Parse<BrokerType>(requestBody.BrokerType);

            var command = new RegisterAccountCommand(accountId)
            {
                UserId = userId,
                BrokerType = brokerType,
                BrokerName = requestBody.BrokerName,
                AccountNumber = requestBody.AccountNumber,
                ServerName = requestBody.ServerName,
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            // Query the registered account
            var query = new GetAccountByIdQuery(accountId);
            var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            if (account == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ApiResponse("Failed to register account"));
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(MapToAccountResponse(account));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// GET /api/accounts - Get all accounts for a user
    /// </summary>
    [Function("GetUserAccounts")]
    [OpenApiOperation("GetUserAccounts", new[] { "Accounts" }, Summary = "Get all accounts for a user")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter("userId", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "User ID (admin only)")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(IEnumerable<AccountResponse>))]
    public async Task<HttpResponseData> GetUserAccounts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts")] HttpRequestData req)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            // Get userId from query parameters or use current user
            var queryUserId = req.Query["userId"];
            var userId = !string.IsNullOrEmpty(queryUserId) ? queryUserId : tokenValues.UserId;

            // Check authorization: user can view their own accounts or admin can view any
            if (userId != tokenValues.UserId && !await _authorizationService.IsAdminAsync(tokenValues.UserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var query = new GetAccountsByUserIdQuery(new UserId(userId));
            var accounts = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            var accountResponses = accounts.Select(MapToAccountResponse).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(accountResponses);
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// GET /api/accounts/{accountId} - Get account details
    /// </summary>
    [Function("GetAccountById")]
    [OpenApiOperation("GetAccountById", new[] { "Accounts" }, Summary = "Get account details")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter("accountId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Account ID")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiResponse), Description = "Not found")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(AccountResponse))]
    public async Task<HttpResponseData> GetAccountById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var query = new GetAccountByIdQuery(new AccountId(accountId));
            var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            if (account == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new ApiResponse("Account not found"));
                return notFoundResponse;
            }

            // Check authorization: owner or admin
            if (account.UserId != tokenValues.UserId && !await _authorizationService.IsAdminAsync(tokenValues.UserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(MapToAccountResponse(account));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// PUT /api/accounts/{accountId} - Update account information
    /// </summary>
    [Function("UpdateAccount")]
    [OpenApiOperation("UpdateAccount", new[] { "Accounts" }, Summary = "Update account information")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter("accountId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Account ID")]
    [OpenApiRequestBody("application/json", typeof(UpdateAccountRequest), Description = "Account update request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiResponse), Description = "Not found")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(AccountResponse))]
    public async Task<HttpResponseData> UpdateAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            // Check account exists and user has permission
            var getQuery = new GetAccountByIdQuery(new AccountId(accountId));
            var account = await _queryProcessor.ProcessAsync(getQuery, CancellationToken.None);

            if (account == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new ApiResponse("Account not found"));
                return notFoundResponse;
            }

            if (account.UserId != tokenValues.UserId && !await _authorizationService.IsAdminAsync(tokenValues.UserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var requestBody = await req.ReadAsJsonAsync<UpdateAccountRequest>();
            if (requestBody == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Invalid request body"));
                return badResponse;
            }

            var command = new UpdateAccountInfoCommand(new AccountId(accountId))
            {
                BrokerType = string.IsNullOrEmpty(requestBody.BrokerType) ? null : Enum.Parse<BrokerType>(requestBody.BrokerType),
                BrokerName = requestBody.BrokerName,
                AccountNumber = requestBody.AccountNumber,
                ServerName = requestBody.ServerName,
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            // Query the updated account
            var updatedAccount = await _queryProcessor.ProcessAsync(getQuery, CancellationToken.None);

            if (updatedAccount == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ApiResponse("Failed to update account"));
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(MapToAccountResponse(updatedAccount));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// DELETE /api/accounts/{accountId} - Delete an account
    /// </summary>
    [Function("DeleteAccount")]
    [OpenApiOperation("DeleteAccount", new[] { "Accounts" }, Summary = "Delete an account")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter("accountId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Account ID")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Forbidden")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Account deleted")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(ApiResponse), Description = "Not found")]
    public async Task<HttpResponseData> DeleteAccount(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "accounts/{accountId}")] HttpRequestData req,
        string accountId)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            // Check account exists and user has permission
            var getQuery = new GetAccountByIdQuery(new AccountId(accountId));
            var account = await _queryProcessor.ProcessAsync(getQuery, CancellationToken.None);

            if (account == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new ApiResponse("Account not found"));
                return notFoundResponse;
            }

            if (account.UserId != tokenValues.UserId && !await _authorizationService.IsAdminAsync(tokenValues.UserId))
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                return forbiddenResponse;
            }

            var command = new DeleteAccountCommand(new AccountId(accountId));
            await _commandBus.PublishAsync(command, CancellationToken.None);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// POST /api/accounts/activation/initiate - Initiate account activation (Flow 2)
    /// </summary>
    [Function("InitiateActivation")]
    [OpenApiOperation("InitiateActivation", new[] { "Accounts" }, Summary = "Initiate account activation")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(InitiateActivationRequest), Description = "Activation initiation request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ActivationCodeResponse))]
    public async Task<HttpResponseData> InitiateActivation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts/activation/initiate")] HttpRequestData req)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var requestBody = await req.ReadAsJsonAsync<InitiateActivationRequest>();
            if (requestBody == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Invalid request body"));
                return badResponse;
            }

            var activationCodeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
            var userId = new UserId(tokenValues.UserId);

            var command = new GenerateActivationCodeCommand(activationCodeId)
            {
                Code = GenerateActivationCode(),
                BrokerType = Enum.Parse<BrokerType>(requestBody.BrokerType),
                BrokerName = requestBody.BrokerName,
                AccountNumber = requestBody.AccountNumber,
                ServerName = requestBody.ServerName,
                UserId = userId,
            };

            await _commandBus.PublishAsync(command, CancellationToken.None);

            // Query the generated activation code
            var query = new GetActivationCodeByIdQuery(activationCodeId);
            var activationCode = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

            if (activationCode == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ApiResponse("Failed to generate activation code"));
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(MapToActivationCodeResponse(activationCode));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    /// <summary>
    /// POST /api/accounts/activation/confirm - Confirm account activation
    /// </summary>
    [Function("ConfirmActivation")]
    [OpenApiOperation("ConfirmActivation", new[] { "Accounts" }, Summary = "Confirm account activation")]
    [OpenApiSecurity("BearerAuth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody("application/json", typeof(ActivationConfirmRequest), Description = "Activation confirmation request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Unauthorized")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ApiResponse), Description = "Bad request")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(AccountResponse))]
    public async Task<HttpResponseData> ConfirmActivation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "accounts/activation/confirm")] HttpRequestData req)
    {
        try
        {
            _requestDataAccessor.SetHttpRequestData(req);
            var tokenValues = req.ExtractJwtTokenValues();
            if (tokenValues == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                return unauthorizedResponse;
            }

            var requestBody = await req.ReadAsJsonAsync<ActivationConfirmRequest>();
            if (requestBody == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Invalid request body"));
                return badResponse;
            }

            if (string.IsNullOrEmpty(requestBody.ActivationCode))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Invalid request body"));
                return badResponse;
            }


            var codeQuery = new GetActivationCodeByCodeQuery(requestBody.ActivationCode);
            var activationCode = await _queryProcessor.ProcessAsync(codeQuery, CancellationToken.None);
            if (activationCode == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse("Activation code not found or invalid"));
                return badResponse;
            }

            // Confirm the activation code
            var confirmCommand = new ConfirmActivationCodeCommand(new ActivationCodeId(activationCode.Id))
            {
                UserId = new UserId(tokenValues.UserId)
            };

            await _commandBus.PublishAsync(confirmCommand, CancellationToken.None);

            // Register the account based on activation code info
            var accountId = new AccountId($"account-{Guid.NewGuid()}");
            var registerCommand = new RegisterAccountCommand(accountId)
            {
                UserId = new UserId(tokenValues.UserId),
                BrokerType = Enum.Parse<BrokerType>(activationCode.BrokerType), // Default, should be determined by flow
                BrokerName = activationCode.BrokerName,
                AccountNumber = activationCode.AccountNumber,
                ServerName = activationCode.ServerName
            };

            await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

            // Query the registered account
            var accountQuery = new GetAccountByIdQuery(accountId);
            var account = await _queryProcessor.ProcessAsync(accountQuery, CancellationToken.None);

            if (account == null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ApiResponse("Failed to confirm activation"));
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(MapToAccountResponse(account));
            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new ApiResponse(ex.Message));
            return errorResponse;
        }
        finally
        {
            _requestDataAccessor.ClearHttpRequestData();
        }
    }

    #region Helper Methods

    private static AccountResponse MapToAccountResponse(AccountReadModel account)
    {
        return new AccountResponse
        {
            AccountId = account.Id,
            UserId = account.UserId,
            BrokerType = account.BrokerType,
            BrokerName = account.BrokerName,
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName,
            ConnectionStatus = account.ConnectionStatus,
            Balance = account.Balance,
            RegisteredAt = account.CreatedAt,
            LastConnectionTestAt = account.LastVerifiedAt,
            IsDeleted = account.IsDeleted
        };
    }

    private static ActivationCodeResponse MapToActivationCodeResponse(ActivationCodeReadModel code)
    {
        return new ActivationCodeResponse
        {
            ActivationCodeId = code.Id,
            Code = code.Code,
            BrokerName = code.BrokerName,
            AccountNumber = code.AccountNumber,
            ServerName = code.ServerName,
            UserId = code.UserId,
            Status = code.Status.ToString(),
            ExpiresAt = code.ExpiresAt,
            GeneratedAt = code.GeneratedAt,
            ConfirmedAt = code.ConfirmedAt
        };
    }

    private static string GenerateActivationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 9).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    #endregion
}
