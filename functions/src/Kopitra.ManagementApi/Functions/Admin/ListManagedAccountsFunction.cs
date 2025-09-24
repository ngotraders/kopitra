using System.Net;
using Kopitra.ManagementApi.Accounts;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.Admin;

public sealed class ListManagedAccountsFunction
{
    private readonly IAccountService _accountService;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListManagedAccountsFunction(IAccountService accountService, AdminRequestContextFactory contextFactory)
    {
        _accountService = accountService;
        _contextFactory = contextFactory;
    }

    [Function("ListManagedAccounts")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/accounts")] HttpRequestData request,
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

        var accounts = await _accountService.ListAccountsAsync(context.TenantId, cancellationToken);
        var payload = new ManagedAccountListResponse(accounts.Count, accounts);
        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, payload, cancellationToken);
    }
}
