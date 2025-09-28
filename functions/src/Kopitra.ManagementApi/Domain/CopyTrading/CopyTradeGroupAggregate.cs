using System;
using System.Collections.Generic;
using EventFlow.Aggregates;

namespace Kopitra.ManagementApi.Domain.CopyTrading;

public sealed class CopyTradeGroupAggregate : AggregateRoot<CopyTradeGroupAggregate, CopyTradeGroupId>
{
    private readonly Dictionary<string, GroupMemberState> _members = new();

    public CopyTradeGroupAggregate(CopyTradeGroupId id) : base(id)
    {
    }

    public string TenantId { get; private set; } = string.Empty;
    public string BusinessId { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyDictionary<string, GroupMemberState> Members => _members;

    public void Create(string tenantId, string groupId, string name, string? description, string createdBy, DateTimeOffset createdAt)
    {
        if (!string.IsNullOrEmpty(TenantId))
        {
            throw new InvalidOperationException($"Copy trade group {groupId} already exists.");
        }

        Emit(new CopyTradeGroupCreated(tenantId, groupId, name, description, createdBy, createdAt));
    }

    public void UpsertMember(string memberId, CopyTradeMemberRole role, RiskStrategy riskStrategy, decimal allocation, DateTimeOffset updatedAt, string updatedBy)
    {
        if (string.IsNullOrEmpty(TenantId))
        {
            throw new InvalidOperationException("Group must be created before managing members.");
        }

        Emit(new CopyTradeGroupMemberUpserted(TenantId, BusinessId, memberId, role, riskStrategy, allocation, updatedAt, updatedBy));
    }

    public void RemoveMember(string memberId, DateTimeOffset removedAt, string removedBy)
    {
        if (!_members.ContainsKey(memberId))
        {
            return;
        }

        Emit(new CopyTradeGroupMemberRemoved(TenantId, BusinessId, memberId, removedAt, removedBy));
    }

    private void Apply(CopyTradeGroupCreated @event)
    {
        TenantId = @event.TenantId;
        BusinessId = @event.GroupId;
        GroupName = @event.Name;
        Description = @event.Description;
        CreatedBy = @event.CreatedBy;
        CreatedAt = @event.CreatedAt;
    }

    private void Apply(CopyTradeGroupMemberUpserted @event)
    {
        _members[@event.MemberId] = new GroupMemberState(@event.MemberId, @event.Role, @event.RiskStrategy, @event.Allocation, @event.UpdatedAt, @event.UpdatedBy);
    }

    private void Apply(CopyTradeGroupMemberRemoved @event)
    {
        _members.Remove(@event.MemberId);
    }

    public sealed record GroupMemberState(string MemberId, CopyTradeMemberRole Role, RiskStrategy RiskStrategy, decimal Allocation, DateTimeOffset UpdatedAt, string UpdatedBy);
}
