using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Subscribers;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class ExpertAdvisorProjection :
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorRegistered>,
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorApproved>,
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorStatusChanged>
{
    private readonly IExpertAdvisorReadModelStore _store;

    public ExpertAdvisorProjection(IExpertAdvisorReadModelStore store)
    {
        _store = store;
    }

    public Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorRegistered> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var model = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, @event.DisplayName, @event.Description, ExpertAdvisorStatus.PendingApproval, null, domainEvent.Timestamp);
        return _store.UpsertAsync(model, cancellationToken);
    }

    public async Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorApproved> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, string.Empty, string.Empty, ExpertAdvisorStatus.Approved, @event.ApprovedBy, domainEvent.Timestamp);
        }

        var updated = existing with { Status = ExpertAdvisorStatus.Approved, ApprovedBy = @event.ApprovedBy, UpdatedAt = domainEvent.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorStatusChanged> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        var existing = await _store.GetAsync(@event.TenantId, @event.ExpertAdvisorId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new ExpertAdvisorReadModel(@event.TenantId, @event.ExpertAdvisorId, string.Empty, string.Empty, @event.Status, null, domainEvent.Timestamp);
        }

        var updated = existing with { Status = @event.Status, UpdatedAt = domainEvent.Timestamp };
        await _store.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
