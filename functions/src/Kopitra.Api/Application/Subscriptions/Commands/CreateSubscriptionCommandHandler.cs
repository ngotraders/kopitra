using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class CreateSubscriptionCommandHandler : CommandHandler<SubscriptionAggregate, SubscriptionId, CreateSubscriptionCommand>
{
    public override Task ExecuteAsync(
        SubscriptionAggregate aggregate,
        CreateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.Create(
            command.SubscriberId,
            command.ProviderId,
            command.SubscriberAccountId,
            command.FundingStrategy,
            command.MaxEquityRisk);
        return Task.CompletedTask;
    }
}
