using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Subscribers;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Messaging;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class ExpertAdvisorMessagingHandler :
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorRegistered>,
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorApproved>,
    ISubscribeSynchronousTo<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorStatusChanged>
{
    private readonly IServiceBusPublisher _publisher;

    public ExpertAdvisorMessagingHandler(IServiceBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorRegistered> domainEvent, CancellationToken cancellationToken)
    {
        var payload = new
        {
            domainEvent.AggregateEvent.TenantId,
            domainEvent.AggregateEvent.ExpertAdvisorId,
            domainEvent.AggregateEvent.DisplayName,
            domainEvent.AggregateEvent.Description,
            domainEvent.AggregateEvent.RequestedBy,
            domainEvent.AggregateEvent.RegisteredAt,
            Type = "expert-advisor-registered"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }

    public Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorApproved> domainEvent, CancellationToken cancellationToken)
    {
        var payload = new
        {
            domainEvent.AggregateEvent.TenantId,
            domainEvent.AggregateEvent.ExpertAdvisorId,
            domainEvent.AggregateEvent.ApprovedBy,
            domainEvent.AggregateEvent.ApprovedAt,
            Type = "expert-advisor-approved"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }

    public Task HandleAsync(IDomainEvent<ExpertAdvisorAggregate, ExpertAdvisorId, ExpertAdvisorStatusChanged> domainEvent, CancellationToken cancellationToken)
    {
        var payload = new
        {
            domainEvent.AggregateEvent.TenantId,
            domainEvent.AggregateEvent.ExpertAdvisorId,
            domainEvent.AggregateEvent.Status,
            domainEvent.AggregateEvent.Reason,
            domainEvent.AggregateEvent.ChangedAt,
            Type = "expert-advisor-status-changed"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }
}
