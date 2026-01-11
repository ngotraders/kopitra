using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Orders.Events;

/// <summary>
/// Order status
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Executed = 1,
    PartiallyExecuted = 2,
    Canceled = 3,
    Failed = 4
}

/// <summary>
/// Order created event
/// </summary>
public class OrderCreatedEvent : AggregateEvent<OrderAggregate, OrderId>
{
    public SignalId SignalId { get; set; } = null!;
    public AccountId SubscriberAccountId { get; set; } = null!;
    public TradingSymbol Symbol { get; set; } = null!;
    public OrderAction Action { get; set; }
    public decimal Lot { get; set; }
    public RiskManagement RiskManagement { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Order executed event
/// </summary>
public class OrderExecutedEvent : AggregateEvent<OrderAggregate, OrderId>
{
    public decimal ExecutedLot { get; set; }
    public decimal ExecutionPrice { get; set; }
    public DateTime ExecutedAt { get; set; }
}

/// <summary>
/// Order canceled event
/// </summary>
public class OrderCanceledEvent : AggregateEvent<OrderAggregate, OrderId>
{
    public string Reason { get; set; } = null!;
    public DateTime CanceledAt { get; set; }
}

/// <summary>
/// Order modified event (for modify signals)
/// </summary>
public class OrderModifiedEvent : AggregateEvent<OrderAggregate, OrderId>
{
    public decimal? NewLot { get; set; }
    public RiskManagement? NewRiskManagement { get; set; }
    public DateTime ModifiedAt { get; set; }
}
