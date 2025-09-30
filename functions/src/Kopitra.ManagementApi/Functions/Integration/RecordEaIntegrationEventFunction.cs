using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.Integration.Commands;
using Kopitra.ManagementApi.Application.Integration.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.Integration;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.Integration;

public sealed class RecordEaIntegrationEventFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public RecordEaIntegrationEventFunction(
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

    [Function("RecordEaIntegrationEvent")]
    [OpenApiOperation(operationId: "RecordEaIntegrationEvent", tags: new[] { "Integration" }, Summary = "Record EA integration event", Description = "Persists an integration event received from an expert advisor interface.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "Idempotency-Key", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Idempotency key", Description = "Optional key to guarantee exactly-once processing for retried requests.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RecordEaIntegrationEventRequest), Required = true, Description = "Integration event payload from the expert advisor interface.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(EaIntegrationEvent), Summary = "Integration event recorded", Description = "The recorded integration event entry.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/integration/events")] HttpRequestData request,
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

            var payload = JsonSerializer.Deserialize<RecordEaIntegrationEventRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.Source) || string.IsNullOrWhiteSpace(payload.EventType))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "source, eventType, and payload are required.", cancellationToken);
            }

            var hash = InMemoryIdempotencyStore.ComputeHash(body);
            var dedupeKey = context.IdempotencyKey ?? $"{request.FunctionContext.FunctionDefinition.Name}:{hash}";
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, dedupeKey, hash, cancellationToken);
            if (!result.IsNew)
            {
                var existing = await _queryDispatcher.DispatchAsync(new ListEaIntegrationEventsQuery(context.TenantId), cancellationToken);
                return await request.CreateJsonResponseAsync(HttpStatusCode.OK, existing, cancellationToken);
            }

            var command = new RecordEaIntegrationEventCommand(context.TenantId, payload.Source, payload.EventType, payload.Payload, payload.OccurredAt ?? DateTimeOffset.UtcNow);
            var recorded = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.Accepted, recorded, cancellationToken);
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

    private sealed record RecordEaIntegrationEventRequest(
        [property: Required] string Source,
        [property: Required] string EventType,
        [property: Required] string Payload,
        DateTimeOffset? OccurredAt);
}
