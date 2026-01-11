using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Signals.Events;

/// <summary>
/// Signal created event - fired when provider creates a trading signal
/// </summary>
public class SignalCreatedEvent : AggregateEvent<SignalAggregate, SignalId>
{
    public UserId ProviderId { get; set; } = null!;
    public TradingSymbol Symbol { get; set; } = null!;
    public OrderAction Action { get; set; }
    public PositionSize PositionSize { get; set; } = null!;
    public RiskManagement RiskManagement { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Signal modified event - fired when provider modifies an open signal
/// </summary>
public class SignalModifiedEvent : AggregateEvent<SignalAggregate, SignalId>
{
    public PositionSize? NewPositionSize { get; set; }
    public RiskManagement? NewRiskManagement { get; set; }
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// Signal closed event - fired when provider closes a signal
/// </summary>
public class SignalClosedEvent : AggregateEvent<SignalAggregate, SignalId>
{
    public DateTime ClosedAt { get; set; }
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Signal distributed to subscribers event - for tracking distribution
/// </summary>
public class SignalDistributedEvent : AggregateEvent<SignalAggregate, SignalId>
{
    public int DistributedToCount { get; set; }
    public DateTime DistributedAt { get; set; }
}

/// <summary>
/// Signal execution completed event
/// </summary>
public class SignalExecutionCompletedEvent : AggregateEvent<SignalAggregate, SignalId>
{
    public int ExecutedCount { get; set; }
    public decimal AverageExecutionPrice { get; set; }
    public DateTime CompletedAt { get; set; }
}
