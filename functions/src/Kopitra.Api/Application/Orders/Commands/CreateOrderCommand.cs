using EventFlow.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class CreateOrderCommand : Command<OrderAggregate, OrderId>
{
    public SignalId SignalId { get; set; } = null!;
    public AccountId SubscriberAccountId { get; set; } = null!;
    public TradingSymbol Symbol { get; set; } = null!;
    public OrderAction Action { get; set; }
    public decimal Lot { get; set; }
    public RiskManagement RiskManagement { get; set; } = null!;

    public CreateOrderCommand(OrderId aggregateId) : base(aggregateId) { }
}
