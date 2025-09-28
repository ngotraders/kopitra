using System.Net;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.AdminUsers;

public sealed class ListAdminUsersFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListAdminUsersFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("ListAdminUsers")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/users")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: false);
            var result = await _queryDispatcher.DispatchAsync(new ListAdminUsersQuery(context.TenantId), cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
