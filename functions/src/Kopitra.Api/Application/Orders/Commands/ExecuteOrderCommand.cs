using EventFlow.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class ExecuteOrderCommand : Command<OrderAggregate, OrderId>
{
    public decimal ExecutedLot { get; set; }
    public decimal ExecutionPrice { get; set; }

    public ExecuteOrderCommand(OrderId aggregateId) : base(aggregateId) { }
}
