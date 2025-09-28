using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class CopyTradeGroupProjection :
    IDomainEventHandler<CopyTradeGroupCreated>,
    IDomainEventHandler<CopyTradeGroupMemberUpserted>,
    IDomainEventHandler<CopyTradeGroupMemberRemoved>
{
    private readonly ICopyTradeGroupReadModelStore _store;

    public CopyTradeGroupProjection(ICopyTradeGroupReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(DomainEventEnvelope<CopyTradeGroupCreated> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var model = new CopyTradeGroupReadModel(@event.TenantId, @event.GroupId, @event.Name, @event.Description, @event.CreatedBy, @event.CreatedAt, Array.Empty<CopyTradeGroupMemberReadModel>());
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(DomainEventEnvelope<CopyTradeGroupMemberUpserted> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var existing = await _store.GetAsync(@event.TenantId, @event.GroupId, cancellationToken).ConfigureAwait(false);
        var members = existing?.Members?.ToDictionary(m => m.MemberId) ?? new Dictionary<string, CopyTradeGroupMemberReadModel>();
        members[@event.MemberId] = new CopyTradeGroupMemberReadModel(@event.MemberId, @event.Role, @event.RiskStrategy, @event.Allocation, envelope.Timestamp, @event.UpdatedBy);
        var updated = new CopyTradeGroupReadModel(@event.TenantId, @event.GroupId, existing?.Name ?? string.Empty, existing?.Description, existing?.CreatedBy ?? string.Empty, existing?.CreatedAt ?? envelope.Timestamp, members.Values.OrderBy(m => m.MemberId).ToArray());
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(DomainEventEnvelope<CopyTradeGroupMemberRemoved> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
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
