using EventFlow.Commands;
using Kopitra.Api.Application.Orders.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class ExecuteOrderCommandHandler : CommandHandler<OrderAggregate, OrderId, ExecuteOrderCommand>
{
    public override Task ExecuteAsync(OrderAggregate aggregate, ExecuteOrderCommand command, CancellationToken cancellationToken)
    {
        aggregate.Execute(command.ExecutedLot, command.ExecutionPrice);
        return Task.CompletedTask;
    }
}
