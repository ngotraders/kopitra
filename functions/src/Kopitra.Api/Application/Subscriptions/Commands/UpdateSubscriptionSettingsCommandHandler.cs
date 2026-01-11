using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class UpdateSubscriptionSettingsCommandHandler : CommandHandler<SubscriptionAggregate, SubscriptionId, UpdateSubscriptionSettingsCommand>
{
    public override Task ExecuteAsync(
        SubscriptionAggregate aggregate,
        UpdateSubscriptionSettingsCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.UpdateSettings(command.NewFundingStrategy, command.NewMaxEquityRisk);
        return Task.CompletedTask;
    }
}
