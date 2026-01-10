using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Kopitra.Api.Domain.ExpertAdvisors.Events;

namespace Kopitra.Api.Domain.ExpertAdvisors;

public class ExpertAdvisorSessionReadModel : IReadModel,
    IAmReadModelFor<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionCreatedEvent>,
    IAmReadModelFor<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionAuthenticatedEvent>,
    IAmReadModelFor<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, HeartbeatReceivedEvent>,
    IAmReadModelFor<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionClosedEvent>
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public int State { get; set; }
    public string? JwtToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionCreatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        UserId = @event.UserId;
        AccountId = @event.AccountId;
        CreatedAt = domainEvent.Timestamp;
        State = (int)SessionState.Pending;
        ExpiresAt = domainEvent.Timestamp.AddHours(24);
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionAuthenticatedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        JwtToken = @event.JwtToken;
        State = (int)SessionState.Authenticated;
        LastHeartbeatAt = @event.AuthenticatedAt;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, HeartbeatReceivedEvent> domainEvent, CancellationToken cancellationToken)
    {
        var @event = domainEvent.AggregateEvent;
        Id = context.ReadModelId;
        LastHeartbeatAt = @event.ReceivedAt;
        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context, IDomainEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId, SessionClosedEvent> domainEvent, CancellationToken cancellationToken)
    {
        Id = context.ReadModelId;
        State = (int)SessionState.Closed;
        return Task.CompletedTask;
    }
}
