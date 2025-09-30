using System.Collections.Generic;
using System.Net;
using Kopitra.ManagementApi.Application.Integration.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.Integration;

public sealed class ListEaIntegrationEventsFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListEaIntegrationEventsFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("ListEaIntegrationEvents")]
    [OpenApiOperation(operationId: "ListEaIntegrationEvents", tags: new[] { "Integration" }, Summary = "List EA integration events", Description = "Returns the integration events received from expert advisors.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "X-TradeAgent-Account", In = ParameterLocation.Header, Required = true, Type = typeof(string), Summary = "Tenant identifier", Description = "Specifies the tenant scope for the request.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "X-TradeAgent-Request-ID", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Correlation identifier", Description = "Propagated request identifier for tracing.")]
    [OpenApiParameter(name: "X-TradeAgent-Sandbox", In = ParameterLocation.Header, Required = false, Type = typeof(bool), Summary = "Sandbox flag", Description = "Marks the request for sandbox-only processing.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<EaIntegrationEvent>), Summary = "EA integration events", Description = "The integration events stored for the tenant.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers are invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/integration/events")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: false);
            var result = await _queryDispatcher.DispatchAsync(new ListEaIntegrationEventsQuery(context.TenantId), cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
