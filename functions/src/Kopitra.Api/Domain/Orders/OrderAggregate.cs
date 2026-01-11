using EventFlow.Aggregates;
using Kopitra.Api.Domain.Orders.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Orders;

/// <summary>
/// Order aggregate - represents a copied trade order on subscriber account
/// </summary>
public class OrderAggregate : AggregateRoot<OrderAggregate, OrderId>
{
    public SignalId SignalId { get; private set; } = null!;
    public AccountId SubscriberAccountId { get; private set; } = null!;
    public TradingSymbol Symbol { get; private set; } = null!;
    public OrderAction Action { get; private set; }
    public decimal Lot { get; private set; }
    public RiskManagement RiskManagement { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public decimal ExecutedLot { get; private set; }
    public decimal? ExecutionPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExecutedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    public OrderAggregate(OrderId id) : base(id)
    {
    }

    /// <summary>
    /// Create new order from signal
    /// </summary>
    public void Create(
        SignalId signalId,
        AccountId subscriberAccountId,
        TradingSymbol symbol,
        OrderAction action,
        decimal lot,
        RiskManagement riskManagement)
    {
        if (signalId == null)
            throw new ArgumentNullException(nameof(signalId));
        if (subscriberAccountId == null)
            throw new ArgumentNullException(nameof(subscriberAccountId));
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if (lot <= 0)
            throw new ArgumentException("Lot must be greater than zero.", nameof(lot));
        if (riskManagement == null)
            throw new ArgumentNullException(nameof(riskManagement));

        Emit(new OrderCreatedEvent
        {
            SignalId = signalId,
            SubscriberAccountId = subscriberAccountId,
            Symbol = symbol,
            Action = action,
            Lot = lot,
            RiskManagement = riskManagement,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Record order execution
    /// </summary>
    public void Execute(decimal executedLot, decimal executionPrice)
    {
        if (Status == OrderStatus.Executed || Status == OrderStatus.Canceled)
            throw new InvalidOperationException($"Cannot execute order in {Status} status.");
        if (executedLot <= 0)
            throw new ArgumentException("Executed lot must be greater than zero.", nameof(executedLot));
        if (executionPrice <= 0)
            throw new ArgumentException("Execution price must be greater than zero.", nameof(executionPrice));

        Emit(new OrderExecutedEvent
        {
            ExecutedLot = executedLot,
            ExecutionPrice = executionPrice,
            ExecutedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Modify order (in response to signal modification)
    /// </summary>
    public void Modify(decimal? newLot = null, RiskManagement? newRiskManagement = null)
    {
        if (Status == OrderStatus.Executed || Status == OrderStatus.Canceled)
            throw new InvalidOperationException($"Cannot modify order in {Status} status.");
        if (newLot.HasValue && newLot.Value <= 0)
            throw new ArgumentException("New lot must be greater than zero.", nameof(newLot));
        if (newLot == null && newRiskManagement == null)
            throw new ArgumentException("At least one parameter must be provided for modification.");

        Emit(new OrderModifiedEvent
        {
            NewLot = newLot,
            NewRiskManagement = newRiskManagement,
            ModifiedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    public void Cancel(string reason = "Signal closed")
    {
        if (Status == OrderStatus.Executed || Status == OrderStatus.Canceled)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status.");

        Emit(new OrderCanceledEvent
        {
            Reason = reason,
            CanceledAt = DateTime.UtcNow
        });
    }

    private void Apply(OrderCreatedEvent domainEvent)
    {
        SignalId = domainEvent.SignalId;
        SubscriberAccountId = domainEvent.SubscriberAccountId;
        Symbol = domainEvent.Symbol;
        Action = domainEvent.Action;
        Lot = domainEvent.Lot;
        RiskManagement = domainEvent.RiskManagement;
        Status = OrderStatus.Pending;
        CreatedAt = domainEvent.CreatedAt;
    }

    private void Apply(OrderExecutedEvent domainEvent)
    {
        ExecutedLot = domainEvent.ExecutedLot;
        ExecutionPrice = domainEvent.ExecutionPrice;
        ExecutedAt = domainEvent.ExecutedAt;
        Status = ExecutedLot >= Lot ? OrderStatus.Executed : OrderStatus.PartiallyExecuted;
    }

    private void Apply(OrderCanceledEvent domainEvent)
    {
        Status = OrderStatus.Canceled;
        CanceledAt = domainEvent.CanceledAt;
    }

    private void Apply(OrderModifiedEvent domainEvent)
    {
        if (domainEvent.NewLot.HasValue)
            Lot = domainEvent.NewLot.Value;
        if (domainEvent.NewRiskManagement != null)
            RiskManagement = domainEvent.NewRiskManagement;
    }
}
