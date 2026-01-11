using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class PauseSubscriptionCommandHandler : CommandHandler<SubscriptionAggregate, SubscriptionId, PauseSubscriptionCommand>
{
    public override Task ExecuteAsync(
        SubscriptionAggregate aggregate,
        PauseSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.Pause();
        return Task.CompletedTask;
    }
}
