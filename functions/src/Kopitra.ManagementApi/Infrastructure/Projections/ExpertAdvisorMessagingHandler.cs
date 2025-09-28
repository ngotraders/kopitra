using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Messaging;

namespace Kopitra.ManagementApi.Infrastructure.Projections;

public sealed class ExpertAdvisorMessagingHandler :
    IDomainEventHandler<ExpertAdvisorRegistered>,
    IDomainEventHandler<ExpertAdvisorApproved>,
    IDomainEventHandler<ExpertAdvisorStatusChanged>
{
    private readonly IServiceBusPublisher _publisher;

    public ExpertAdvisorMessagingHandler(IServiceBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task HandleAsync(DomainEventEnvelope<ExpertAdvisorRegistered> envelope, CancellationToken cancellationToken)
    {
        var payload = new
        {
            envelope.Event.TenantId,
            envelope.Event.ExpertAdvisorId,
            envelope.Event.DisplayName,
            envelope.Event.Description,
            envelope.Event.RequestedBy,
            envelope.Event.RegisteredAt,
            Type = "expert-advisor-registered"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }

    public Task HandleAsync(DomainEventEnvelope<ExpertAdvisorApproved> envelope, CancellationToken cancellationToken)
    {
        var payload = new
        {
            envelope.Event.TenantId,
            envelope.Event.ExpertAdvisorId,
            envelope.Event.ApprovedBy,
            envelope.Event.ApprovedAt,
            Type = "expert-advisor-approved"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }

    public Task HandleAsync(DomainEventEnvelope<ExpertAdvisorStatusChanged> envelope, CancellationToken cancellationToken)
    {
        var payload = new
        {
            envelope.Event.TenantId,
            envelope.Event.ExpertAdvisorId,
            envelope.Event.Status,
            envelope.Event.Reason,
            envelope.Event.ChangedAt,
            Type = "expert-advisor-status-changed"
        };
        return _publisher.PublishAsync("expert-advisors", payload, cancellationToken);
    }
}
