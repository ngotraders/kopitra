using Kopitra.Cqrs.Events;

namespace Kopitra.ManagementApi.Domain.CopyTrading;

public sealed record CopyTradeGroupCreated(
    string TenantId,
    string GroupId,
    string Name,
    string? Description,
    string CreatedBy,
    DateTimeOffset CreatedAt) : IDomainEvent;

public sealed record CopyTradeGroupMemberUpserted(
    string TenantId,
    string GroupId,
    string MemberId,
    CopyTradeMemberRole Role,
    RiskStrategy RiskStrategy,
    decimal Allocation,
    DateTimeOffset UpdatedAt,
    string UpdatedBy) : IDomainEvent;

public sealed record CopyTradeGroupMemberRemoved(
    string TenantId,
    string GroupId,
    string MemberId,
    DateTimeOffset RemovedAt,
    string RemovedBy) : IDomainEvent;
