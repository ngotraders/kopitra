using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class ExpertAdvisorProjection :
    IDomainEventHandler<ExpertAdvisorRegistered>,
    IDomainEventHandler<ExpertAdvisorApproved>,
    IDomainEventHandler<ExpertAdvisorStatusChanged>
{
    private readonly IExpertAdvisorReadModelStore _store;

    public ExpertAdvisorProjection(IExpertAdvisorReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(DomainEventEnvelope<ExpertAdvisorRegistered> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var model = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, @event.DisplayName, @event.Description, ExpertAdvisorStatus.PendingApproval, null, envelope.Timestamp);
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(DomainEventEnvelope<ExpertAdvisorApproved> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var existing = await _store.GetAsync(@event.TenantId, @event.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, string.Empty, string.Empty, ExpertAdvisorStatus.Approved, @event.ApprovedBy, envelope.Timestamp);
        }

        var updated = existing with { Status = ExpertAdvisorStatus.Approved, ApprovedBy = @event.ApprovedBy, UpdatedAt = envelope.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(DomainEventEnvelope<ExpertAdvisorStatusChanged> envelope, CancellationToken cancellationToken)
    {
        var @event = envelope.Event;
        var existing = await _store.GetAsync(@event.TenantId, @event.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, string.Empty, string.Empty, @event.Status, null, envelope.Timestamp);
        }

        var updated = existing with { Status = @event.Status, UpdatedAt = envelope.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
