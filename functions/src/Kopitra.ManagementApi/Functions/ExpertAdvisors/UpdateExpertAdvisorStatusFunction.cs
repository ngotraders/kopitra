using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class UpdateExpertAdvisorStatusFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public UpdateExpertAdvisorStatusFunction(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        AdminRequestContextFactory contextFactory,
        IIdempotencyStore idempotencyStore)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
        _idempotencyStore = idempotencyStore;
    }

    [Function("UpdateExpertAdvisorStatus")]
    [OpenApiOperation(operationId: "UpdateExpertAdvisorStatus", tags: new[] { "ExpertAdvisors" }, Summary = "Update expert advisor status", Description = "Changes the lifecycle status of an expert advisor.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "expertAdvisorId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Expert advisor identifier", Description = "The identifier of the expert advisor to update.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "Idempotency-Key", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Idempotency key", Description = "Optional key to guarantee exactly-once processing for retried requests.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateExpertAdvisorStatusRequest), Required = true, Description = "Status change payload including the desired state.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ExpertAdvisorReadModel), Summary = "Expert advisor updated", Description = "The updated expert advisor read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/experts/{expertAdvisorId}/status")] HttpRequestData request,
        string expertAdvisorId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<UpdateExpertAdvisorStatusRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "status and requestedBy are required.", cancellationToken);
            }

            if (!Enum.TryParse<ExpertAdvisorStatus>(payload.Status, ignoreCase: true, out var status))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_status", $"Unsupported status value '{payload.Status}'.", cancellationToken);
            }

            var hash = InMemoryIdempotencyStore.ComputeHash(body);
            var dedupeKey = context.IdempotencyKey ?? $"{request.FunctionContext.FunctionDefinition.Name}:{hash}";
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, dedupeKey, hash, cancellationToken);
            if (!result.IsNew)
            {
                var existing = await _queryDispatcher.DispatchAsync(new GetExpertAdvisorQuery(context.TenantId, expertAdvisorId), cancellationToken);
                if (existing is null)
                {
                    return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "expert_not_found", "Expert advisor not found.", cancellationToken);
                }

                return await request.CreateJsonResponseAsync(HttpStatusCode.OK, existing, cancellationToken);
            }

            var command = new UpdateExpertAdvisorStatusCommand(context.TenantId, expertAdvisorId, status, payload.Reason, payload.RequestedBy);
            var readModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, readModel, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.Conflict, "operation_conflict", ex.Message, cancellationToken);
        }
    }

    private sealed record UpdateExpertAdvisorStatusRequest(
        [property: Required] string Status,
        string? Reason,
        [property: Required] string RequestedBy);
}
