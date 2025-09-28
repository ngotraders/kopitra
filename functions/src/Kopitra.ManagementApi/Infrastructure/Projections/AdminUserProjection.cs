using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class AdminUserProjection :
    IDomainEventHandler<AdminUserProvisioned>,
    IDomainEventHandler<AdminUserRolesUpdated>,
    IDomainEventHandler<AdminUserNotificationSettingsUpdated>
{
    private readonly IAdminUserReadModelStore _store;

    public AdminUserProjection(IAdminUserReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(DomainEventEnvelope<AdminUserProvisioned> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var model = new AdminUserReadModel(@event.TenantId, @event.UserId, @event.Email, @event.DisplayName, @event.Roles.ToArray(), false, Array.Empty<string>(), envelope.Timestamp);
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(DomainEventEnvelope<AdminUserRolesUpdated> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var existing = await _store.GetAsync(@event.TenantId, @event.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new AdminUserReadModel(@event.TenantId, @event.UserId, string.Empty, string.Empty, Array.Empty<AdminUserRole>(), false, Array.Empty<string>(), envelope.Timestamp);
        }

        var updated = existing with { Roles = @event.Roles.ToArray(), UpdatedAt = envelope.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(DomainEventEnvelope<AdminUserNotificationSettingsUpdated> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var existing = await _store.GetAsync(@event.TenantId, @event.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new AdminUserReadModel(@event.TenantId, @event.UserId, string.Empty, string.Empty, Array.Empty<AdminUserRole>(), @event.EmailEnabled, @event.Topics.ToArray(), envelope.Timestamp);
        }

        var updated = existing with { EmailEnabled = @event.EmailEnabled, Topics = @event.Topics.ToArray(), UpdatedAt = envelope.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
