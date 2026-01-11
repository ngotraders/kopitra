using EventFlow.Aggregates;
using Kopitra.Api.Domain.ExpertAdvisors.Events;

namespace Kopitra.Api.Domain.ExpertAdvisors;

public enum SessionState
{
    Idle = 0,
    Pending = 1,
    Authenticated = 2,
    Closed = 3
}

public class ExpertAdvisorSessionAggregate : AggregateRoot<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>,
    IEmit<SessionCreatedEvent>,
    IEmit<SessionAuthenticatedEvent>,
    IEmit<HeartbeatReceivedEvent>,
    IEmit<SessionClosedEvent>
{
    private SessionState _state = SessionState.Idle;

    public string UserId { get; private set; } = "";
    public string AccountId { get; private set; } = "";
    public string? JwtToken { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastHeartbeatAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public ExpertAdvisorSessionAggregate(ExpertAdvisorSessionId id) : base(id)
    {
    }

    public void Create(string userId, string accountId, DateTimeOffset createdAt)
    {
        Emit(new SessionCreatedEvent(Id, userId, accountId, createdAt));
        Emit(new HeartbeatReceivedEvent(Id, createdAt));
    }

    public void Authenticate(DateTimeOffset authenticatedAt, string jwtToken)
    {
        if (_state != SessionState.Idle && _state != SessionState.Pending)
        {
            throw new InvalidOperationException($"Cannot authenticate session in state {_state}");
        }
        Emit(new SessionAuthenticatedEvent(Id, jwtToken, authenticatedAt));
    }

    public void RecordHeartbeat(DateTimeOffset receivedAt)
    {
        if (_state == SessionState.Closed)
        {
            throw new InvalidOperationException("Cannot record heartbeat for closed session");
        }
        Emit(new HeartbeatReceivedEvent(Id, receivedAt));
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (_state == SessionState.Closed)
        {
            return;
        }
        Emit(new SessionClosedEvent(Id, closedAt));
    }

    public void Apply(SessionCreatedEvent @event)
    {
        UserId = @event.UserId;
        AccountId = @event.AccountId;
        CreatedAt = @event.CreatedAt;
        _state = SessionState.Pending;
        ExpiresAt = @event.CreatedAt.AddHours(24);
    }

    public void Apply(SessionAuthenticatedEvent @event)
    {
        JwtToken = @event.JwtToken;
        _state = SessionState.Authenticated;
        LastHeartbeatAt = @event.AuthenticatedAt;
    }

    public void Apply(HeartbeatReceivedEvent @event)
    {
        LastHeartbeatAt = @event.ReceivedAt;
    }

    public void Apply(SessionClosedEvent @event)
    {
        _state = SessionState.Closed;
    }

    // JWTs are produced by the application layer (JwtService); keep aggregate focused on state
}
