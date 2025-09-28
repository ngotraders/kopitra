using System;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.Cqrs.Dispatching;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class UpsertCopyTradeGroupMemberFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public UpsertCopyTradeGroupMemberFunction(
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

    [Function("UpsertCopyTradeGroupMember")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "admin/copy-trade/groups/{groupId}/members/{memberId}")] HttpRequestData request,
        string groupId,
        string memberId,
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

            var payload = JsonSerializer.Deserialize<UpsertCopyTradeGroupMemberRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "role, riskStrategy, allocation, and requestedBy are required.", cancellationToken);
            }

            if (!Enum.TryParse<CopyTradeMemberRole>(payload.Role, true, out var role))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_role", $"Unsupported role '{payload.Role}'.", cancellationToken);
            }

            if (!Enum.TryParse<RiskStrategy>(payload.RiskStrategy, true, out var riskStrategy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_risk", $"Unsupported risk strategy '{payload.RiskStrategy}'.", cancellationToken);
            }

            if (payload.Allocation <= 0)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_allocation", "Allocation must be greater than zero.", cancellationToken);
            }

            var hash = InMemoryIdempotencyStore.ComputeHash(body);
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, context.IdempotencyKey!, hash, cancellationToken);
            if (!result.IsNew)
            {
                var existing = await _queryDispatcher.DispatchAsync(new GetCopyTradeGroupQuery(context.TenantId, groupId), cancellationToken);
                if (existing is null)
                {
                    return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "group_not_found", "Copy trade group not found.", cancellationToken);
                }

                return await request.CreateJsonResponseAsync(HttpStatusCode.OK, existing, cancellationToken);
            }

            var command = new UpsertCopyTradeGroupMemberCommand(context.TenantId, groupId, memberId, role, riskStrategy, payload.Allocation, payload.RequestedBy);
            var resultModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, resultModel, cancellationToken);
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

    private sealed record UpsertCopyTradeGroupMemberRequest(string Role, string RiskStrategy, decimal Allocation, string RequestedBy);
}
