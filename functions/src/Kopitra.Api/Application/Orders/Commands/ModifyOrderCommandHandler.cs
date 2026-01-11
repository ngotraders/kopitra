using EventFlow.Commands;
using Kopitra.Api.Application.Orders.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class ModifyOrderCommandHandler : CommandHandler<OrderAggregate, OrderId, ModifyOrderCommand>
{
    public override Task ExecuteAsync(OrderAggregate aggregate, ModifyOrderCommand command, CancellationToken cancellationToken)
    {
        aggregate.Modify(command.NewLot, command.NewRiskManagement);
        return Task.CompletedTask;
    }
}
