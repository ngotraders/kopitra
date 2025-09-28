using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class RemoveCopyTradeGroupMemberFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public RemoveCopyTradeGroupMemberFunction(
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

    [Function("RemoveCopyTradeGroupMember")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "admin/copy-trade/groups/{groupId}/members/{memberId}")] HttpRequestData request,
        string groupId,
        string memberId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: true);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            var payload = string.IsNullOrWhiteSpace(body) ? new RemoveCopyTradeGroupMemberRequest(null) : JsonSerializer.Deserialize<RemoveCopyTradeGroupMemberRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var requestedBy = payload?.RequestedBy ?? "system";

            var hash = InMemoryIdempotencyStore.ComputeHash(body ?? string.Empty);
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

            var command = new RemoveCopyTradeGroupMemberCommand(context.TenantId, groupId, memberId, requestedBy);
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

    private sealed record RemoveCopyTradeGroupMemberRequest(string? RequestedBy);
}
