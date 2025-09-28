using System;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Application.Integration.Commands;
using Kopitra.ManagementApi.Application.Integration.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/integration/events")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: true);
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
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, context.IdempotencyKey!, hash, cancellationToken);
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

    private sealed record RecordEaIntegrationEventRequest(string Source, string EventType, string Payload, DateTimeOffset? OccurredAt);
}
