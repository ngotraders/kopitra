using EventFlow.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class ModifyOrderCommand : Command<OrderAggregate, OrderId>
{
    public decimal NewLot { get; set; }
    public RiskManagement NewRiskManagement { get; set; } = null!;

    public ModifyOrderCommand(OrderId aggregateId) : base(aggregateId) { }
}
