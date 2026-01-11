using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class CancelSubscriptionCommandHandler : CommandHandler<SubscriptionAggregate, SubscriptionId, CancelSubscriptionCommand>
{
    public override Task ExecuteAsync(
        SubscriptionAggregate aggregate,
        CancelSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.Cancel(command.Reason);
        return Task.CompletedTask;
    }
}
