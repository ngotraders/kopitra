using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Gateway;

public interface IGatewayAdminClient
{
    Task ApproveSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? approvedBy,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    Task RejectSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? rejectedBy,
        string? reason,
        CancellationToken cancellationToken);

    Task QueueOutboxEventAsync(
        string accountId,
        Guid sessionId,
        string eventType,
        object payload,
        bool requiresAck,
        CancellationToken cancellationToken);

    Task EnqueueTradeOrderAsync(
        string accountId,
        Guid sessionId,
        TradeOrderCommand command,
        CancellationToken cancellationToken);

    Task<GatewaySessionSummary?> GetActiveSessionAsync(
        string accountId,
        CancellationToken cancellationToken);
}

public sealed record TradeOrderCommand(
    string CommandType,
    string Instrument,
    string? OrderType,
    string? Side,
    double? Volume,
    double? Price,
    double? StopLoss,
    double? TakeProfit,
    string? TimeInForce,
    string? PositionId,
    string? ClientOrderId,
    IDictionary<string, object>? Metadata);

public sealed record GatewaySessionSummary(
    Guid SessionId,
    string AuthKeyFingerprint,
    string Status);
