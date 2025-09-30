using System.Net;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

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
    [OpenApiOperation(operationId: "GetCopyTradeGroup", tags: new[] { "CopyTradeGroups" }, Summary = "Get copy-trade group", Description = "Retrieves the details of a specific copy-trade group.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "groupId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Copy-trade group identifier", Description = "The identifier of the copy-trade group to retrieve.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "X-TradeAgent-Account", In = ParameterLocation.Header, Required = true, Type = typeof(string), Summary = "Tenant identifier", Description = "Specifies the tenant scope for the request.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "X-TradeAgent-Request-ID", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Correlation identifier", Description = "Propagated request identifier for tracing.")]
    [OpenApiParameter(name: "X-TradeAgent-Sandbox", In = ParameterLocation.Header, Required = false, Type = typeof(bool), Summary = "Sandbox flag", Description = "Marks the request for sandbox-only processing.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CopyTradeGroupReadModel), Summary = "Copy-trade group", Description = "The requested copy-trade group details.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Copy-trade group not found", Description = "No copy-trade group exists with the supplied identifier.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers are invalid.")]
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
