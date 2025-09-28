using System.Net;
using Kopitra.Cqrs.Dispatching;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class GetCopyTradeGroupFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public GetCopyTradeGroupFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("GetCopyTradeGroup")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/copy-trade/groups/{groupId}")] HttpRequestData request,
        string groupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: false);
            var result = await _queryDispatcher.DispatchAsync(new GetCopyTradeGroupQuery(context.TenantId, groupId), cancellationToken);
            if (result is null)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "group_not_found", "Copy trade group not found.", cancellationToken);
            }

            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
