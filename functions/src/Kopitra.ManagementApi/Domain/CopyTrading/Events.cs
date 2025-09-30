using System;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Kopitra.ManagementApi.Domain.CopyTrading;

[EventVersion("copy-trade-group-created", 1)]
public sealed class CopyTradeGroupCreated : AggregateEvent<CopyTradeGroupAggregate, CopyTradeGroupId>
{
    public CopyTradeGroupCreated(string tenantId, string groupId, string name, string? description, string createdBy, DateTimeOffset createdAt)
    {
        TenantId = tenantId;
        GroupId = groupId;
        Name = name;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public string TenantId { get; }
    public string GroupId { get; }
    public string Name { get; }
    public string? Description { get; }
    public string CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
}

[EventVersion("copy-trade-group-member-upserted", 1)]
public sealed class CopyTradeGroupMemberUpserted : AggregateEvent<CopyTradeGroupAggregate, CopyTradeGroupId>
{
    public CopyTradeGroupMemberUpserted(string tenantId, string groupId, string memberId, CopyTradeMemberRole role, RiskStrategy riskStrategy, decimal allocation, DateTimeOffset updatedAt, string updatedBy)
    {
        TenantId = tenantId;
        GroupId = groupId;
        MemberId = memberId;
        Role = role;
        RiskStrategy = riskStrategy;
        Allocation = allocation;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public string TenantId { get; }
    public string GroupId { get; }
    public string MemberId { get; }
    public CopyTradeMemberRole Role { get; }
    public RiskStrategy RiskStrategy { get; }
    public decimal Allocation { get; }
    public DateTimeOffset UpdatedAt { get; }
    public string UpdatedBy { get; }
}

[EventVersion("copy-trade-group-member-removed", 1)]
public sealed class CopyTradeGroupMemberRemoved : AggregateEvent<CopyTradeGroupAggregate, CopyTradeGroupId>
{
    public CopyTradeGroupMemberRemoved(string tenantId, string groupId, string memberId, DateTimeOffset removedAt, string removedBy)
    {
        TenantId = tenantId;
        GroupId = groupId;
        MemberId = memberId;
        RemovedAt = removedAt;
        RemovedBy = removedBy;
    }

    public string TenantId { get; }
    public string GroupId { get; }
    public string MemberId { get; }
    public DateTimeOffset RemovedAt { get; }
    public string RemovedBy { get; }
}
