using EventFlow.Commands;
using Kopitra.Api.Application.Orders.Commands;
using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Orders.Commands;

public class CreateOrderCommandHandler : CommandHandler<OrderAggregate, OrderId, CreateOrderCommand>
{
    public override Task ExecuteAsync(OrderAggregate aggregate, CreateOrderCommand command, CancellationToken cancellationToken)
    {
        aggregate.Create(command.SignalId, command.SubscriberAccountId, command.Symbol, command.Action, command.Lot, command.RiskManagement);
        return Task.CompletedTask;
    }
}
