using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Kopitra.Api.Domain.ExpertAdvisors.Events;

[EventVersion("SessionCreated", 1)]
public class SessionCreatedEvent(ExpertAdvisorSessionId id, string userId, string accountId, DateTimeOffset createdAt) : AggregateEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>
{
    public ExpertAdvisorSessionId Id { get; } = id;
    public string UserId { get; } = userId;
    public string AccountId { get; } = accountId;
    public DateTimeOffset CreatedAt { get; } = createdAt;
}

[EventVersion("SessionAuthenticated", 1)]
public class SessionAuthenticatedEvent(ExpertAdvisorSessionId id, string jwtToken, DateTimeOffset authenticatedAt) : AggregateEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>
{
    public ExpertAdvisorSessionId Id { get; } = id;
    public string JwtToken { get; } = jwtToken;
    public DateTimeOffset AuthenticatedAt { get; } = authenticatedAt;
}

[EventVersion("HeartbeatReceived", 1)]
public class HeartbeatReceivedEvent(ExpertAdvisorSessionId id, DateTimeOffset receivedAt) : AggregateEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>
{
    public ExpertAdvisorSessionId Id { get; } = id;
    public DateTimeOffset ReceivedAt { get; } = receivedAt;
}

[EventVersion("SessionClosed", 1)]
public class SessionClosedEvent(ExpertAdvisorSessionId id, DateTimeOffset closedAt) : AggregateEvent<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>
{
    public ExpertAdvisorSessionId Id { get; } = id;
    public DateTimeOffset ClosedAt { get; } = closedAt;
}
