using System.Net;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class ListExpertAdvisorsFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListExpertAdvisorsFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("ListExpertAdvisors")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/experts")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: false);
            var result = await _queryDispatcher.DispatchAsync(new ListExpertAdvisorsQuery(context.TenantId), cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
