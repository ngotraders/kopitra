using EventFlow.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class CancelOrderCommand : Command<OrderAggregate, OrderId>
{
    public string Reason { get; set; } = null!;

    public CancelOrderCommand(OrderId aggregateId) : base(aggregateId) { }
}
