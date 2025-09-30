using System.Net;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class GetExpertAdvisorFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public GetExpertAdvisorFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("GetExpertAdvisor")]
    [OpenApiOperation(operationId: "GetExpertAdvisor", tags: new[] { "ExpertAdvisors" }, Summary = "Get expert advisor", Description = "Retrieves a single expert advisor by identifier.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "expertAdvisorId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Expert advisor identifier", Description = "The identifier of the expert advisor to retrieve.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "X-TradeAgent-Account", In = ParameterLocation.Header, Required = true, Type = typeof(string), Summary = "Tenant identifier", Description = "Specifies the tenant scope for the request.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "X-TradeAgent-Request-ID", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Correlation identifier", Description = "Propagated request identifier for tracing.")]
    [OpenApiParameter(name: "X-TradeAgent-Sandbox", In = ParameterLocation.Header, Required = false, Type = typeof(bool), Summary = "Sandbox flag", Description = "Marks the request for sandbox-only processing.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ExpertAdvisorReadModel), Summary = "Expert advisor", Description = "The requested expert advisor details.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Expert advisor not found", Description = "No expert advisor exists with the supplied identifier.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers are invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/experts/{expertAdvisorId}")] HttpRequestData request,
        string expertAdvisorId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: false);
            var result = await _queryDispatcher.DispatchAsync(new GetExpertAdvisorQuery(context.TenantId, expertAdvisorId), cancellationToken);
            if (result is null)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "expert_not_found", "Expert advisor not found.", cancellationToken);
            }

            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
