using EventFlow.Aggregates;
using Kopitra.Api.Domain.Signals.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Signals;

/// <summary>
/// Signal aggregate - represents a trading signal from provider
/// </summary>
public class SignalAggregate : AggregateRoot<SignalAggregate, SignalId>
{
    public UserId ProviderId { get; private set; } = null!;
    public TradingSymbol Symbol { get; private set; } = null!;
    public OrderAction Action { get; private set; }
    public PositionSize PositionSize { get; private set; } = null!;
    public RiskManagement RiskManagement { get; private set; } = null!;
    public bool IsClosed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public int DistributedToCount { get; private set; }
    public int ExecutedCount { get; private set; }

    public SignalAggregate(SignalId id) : base(id)
    {
    }

    /// <summary>
    /// Create new signal
    /// </summary>
    public void Create(
        UserId providerId,
        TradingSymbol symbol,
        OrderAction action,
        PositionSize positionSize,
        RiskManagement riskManagement)
    {
        if (providerId == null)
            throw new ArgumentNullException(nameof(providerId));
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if (positionSize == null)
            throw new ArgumentNullException(nameof(positionSize));
        if (riskManagement == null)
            throw new ArgumentNullException(nameof(riskManagement));

        Emit(new SignalCreatedEvent
        {
            ProviderId = providerId,
            Symbol = symbol,
            Action = action,
            PositionSize = positionSize,
            RiskManagement = riskManagement,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Modify signal position size or risk management
    /// </summary>
    public void Modify(PositionSize? newPositionSize = null, RiskManagement? newRiskManagement = null)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot modify a closed signal.");
        if (newPositionSize == null && newRiskManagement == null)
            throw new ArgumentException("At least one parameter must be provided for modification.");

        Emit(new SignalModifiedEvent
        {
            NewPositionSize = newPositionSize,
            NewRiskManagement = newRiskManagement,
            ModifiedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Close signal
    /// </summary>
    public void Close(string reason = "Provider closed signal")
    {
        if (IsClosed)
            throw new InvalidOperationException("Signal is already closed.");

        Emit(new SignalClosedEvent
        {
            ClosedAt = DateTime.UtcNow,
            Reason = reason
        });
    }

    /// <summary>
    /// Record signal distribution to subscribers
    /// </summary>
    public void RecordDistribution(int distributedCount)
    {
        if (distributedCount <= 0)
            throw new ArgumentException("Distribution count must be greater than zero.", nameof(distributedCount));

        Emit(new SignalDistributedEvent
        {
            DistributedToCount = distributedCount,
            DistributedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Record signal execution completion
    /// </summary>
    public void RecordExecutionCompletion(int executedCount, decimal averageExecutionPrice)
    {
        if (executedCount <= 0)
            throw new ArgumentException("Executed count must be greater than zero.", nameof(executedCount));
        if (averageExecutionPrice <= 0)
            throw new ArgumentException("Average execution price must be greater than zero.", nameof(averageExecutionPrice));

        Emit(new SignalExecutionCompletedEvent
        {
            ExecutedCount = executedCount,
            AverageExecutionPrice = averageExecutionPrice,
            CompletedAt = DateTime.UtcNow
        });
    }

    private void Apply(SignalCreatedEvent domainEvent)
    {
        ProviderId = domainEvent.ProviderId;
        Symbol = domainEvent.Symbol;
        Action = domainEvent.Action;
        PositionSize = domainEvent.PositionSize;
        RiskManagement = domainEvent.RiskManagement;
        CreatedAt = domainEvent.CreatedAt;
    }

    private void Apply(SignalModifiedEvent domainEvent)
    {
        if (domainEvent.NewPositionSize != null)
            PositionSize = domainEvent.NewPositionSize;
        if (domainEvent.NewRiskManagement != null)
            RiskManagement = domainEvent.NewRiskManagement;
    }

    private void Apply(SignalClosedEvent domainEvent)
    {
        IsClosed = true;
        ClosedAt = domainEvent.ClosedAt;
    }

    private void Apply(SignalDistributedEvent domainEvent)
    {
        DistributedToCount = domainEvent.DistributedToCount;
    }

    private void Apply(SignalExecutionCompletedEvent domainEvent)
    {
        ExecutedCount = domainEvent.ExecutedCount;
    }
}
