using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Gateway;

public sealed class NullGatewayAdminClient : IGatewayAdminClient
{
    public Task ApproveSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? approvedBy,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RejectSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? rejectedBy,
        string? reason,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task QueueOutboxEventAsync(
        string accountId,
        Guid sessionId,
        string eventType,
        object payload,
        bool requiresAck,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task EnqueueTradeOrderAsync(
        string accountId,
        Guid sessionId,
        TradeOrderCommand command,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<GatewaySessionSummary?> GetActiveSessionAsync(
        string accountId,
        CancellationToken cancellationToken) => Task.FromResult<GatewaySessionSummary?>(null);
}
