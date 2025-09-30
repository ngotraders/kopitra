using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Subscribers;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class AdminUserProjection :
    ISubscribeSynchronousTo<AdminUserAggregate, AdminUserId, AdminUserProvisioned>,
    ISubscribeSynchronousTo<AdminUserAggregate, AdminUserId, AdminUserRolesUpdated>,
    ISubscribeSynchronousTo<AdminUserAggregate, AdminUserId, AdminUserNotificationSettingsUpdated>
{
    private readonly IAdminUserReadModelStore _store;

    public AdminUserProjection(IAdminUserReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(IDomainEvent<AdminUserAggregate, AdminUserId, AdminUserProvisioned> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var model = new AdminUserReadModel(@event.TenantId, @event.UserId, @event.Email, @event.DisplayName, @event.Roles.ToArray(), false, Array.Empty<string>(), domainEvent.Timestamp);
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(IDomainEvent<AdminUserAggregate, AdminUserId, AdminUserRolesUpdated> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new AdminUserReadModel(@event.TenantId, @event.UserId, string.Empty, string.Empty, Array.Empty<AdminUserRole>(), false, Array.Empty<string>(), domainEvent.Timestamp);
        }

        var updated = existing with { Roles = @event.Roles.ToArray(), UpdatedAt = domainEvent.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(IDomainEvent<AdminUserAggregate, AdminUserId, AdminUserNotificationSettingsUpdated> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new AdminUserReadModel(@event.TenantId, @event.UserId, string.Empty, string.Empty, Array.Empty<AdminUserRole>(), @event.EmailEnabled, @event.Topics.ToArray(), domainEvent.Timestamp);
        }

        var updated = existing with { EmailEnabled = @event.EmailEnabled, Topics = @event.Topics.ToArray(), UpdatedAt = domainEvent.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
