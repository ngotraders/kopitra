using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Subscribers;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class CopyTradeGroupProjection :
    ISubscribeSynchronousTo<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupCreated>,
    ISubscribeSynchronousTo<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberUpserted>,
    ISubscribeSynchronousTo<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberRemoved>
{
    private readonly ICopyTradeGroupReadModelStore _store;

    public CopyTradeGroupProjection(ICopyTradeGroupReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(IDomainEvent<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupCreated> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var model = new CopyTradeGroupReadModel(@event.TenantId, @event.GroupId, @event.Name, @event.Description, @event.CreatedBy, @event.CreatedAt, Array.Empty<CopyTradeGroupMemberReadModel>());
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(IDomainEvent<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberUpserted> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.GroupId, cancellationToken).ConfigureAwait(false);
        var members = existing?.Members?.ToDictionary(m => m.MemberId) ?? new Dictionary<string, CopyTradeGroupMemberReadModel>();
        members[@event.MemberId] = new CopyTradeGroupMemberReadModel(@event.MemberId, @event.Role, @event.RiskStrategy, @event.Allocation, domainEvent.Timestamp, @event.UpdatedBy);
        var updated = new CopyTradeGroupReadModel(@event.TenantId, @event.GroupId, existing?.Name ?? string.Empty, existing?.Description, existing?.CreatedBy ?? string.Empty, existing?.CreatedAt ?? domainEvent.Timestamp, members.Values.OrderBy(m => m.MemberId).ToArray());
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(IDomainEvent<CopyTradeGroupAggregate, CopyTradeGroupId, CopyTradeGroupMemberRemoved> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.GroupId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        var members = existing.Members.Where(m => !string.Equals(m.MemberId, @event.MemberId, StringComparison.Ordinal)).OrderBy(m => m.MemberId).ToArray();
        var updated = existing with { Members = members };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
