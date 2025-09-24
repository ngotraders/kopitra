using System.Net;
using Kopitra.ManagementApi.Accounts;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.Admin;

public sealed class GetManagedAccountFunction
{
    private readonly IAccountService _accountService;
    private readonly AdminRequestContextFactory _contextFactory;

    public GetManagedAccountFunction(IAccountService accountService, AdminRequestContextFactory contextFactory)
    {
        _accountService = accountService;
        _contextFactory = contextFactory;
    }

    [Function("GetManagedAccount")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/accounts/{accountId}")] HttpRequestData request,
        string accountId,
        CancellationToken cancellationToken)
    {
        AdminRequestContext context;
        try
        {
            context = _contextFactory.Create(request, requireIdempotencyKey: false);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }

        var account = await _accountService.GetAccountAsync(context.TenantId, accountId, cancellationToken);
        if (account is null)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "account_not_found", $"Account '{accountId}' was not found.", cancellationToken);
        }

        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, account, cancellationToken);
    }
}
