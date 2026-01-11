using EventFlow.Commands;
using Kopitra.Api.Application.Orders.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class CancelOrderCommandHandler : CommandHandler<OrderAggregate, OrderId, CancelOrderCommand>
{
    public override Task ExecuteAsync(OrderAggregate aggregate, CancelOrderCommand command, CancellationToken cancellationToken)
    {
        aggregate.Cancel(command.Reason);
        return Task.CompletedTask;
    }
}
