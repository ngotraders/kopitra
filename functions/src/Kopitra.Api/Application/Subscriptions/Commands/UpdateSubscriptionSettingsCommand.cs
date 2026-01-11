using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class UpdateSubscriptionSettingsCommand : Command<SubscriptionAggregate, SubscriptionId>
{
    public FundingStrategy NewFundingStrategy { get; set; }
    public decimal NewMaxEquityRisk { get; set; }

    public UpdateSubscriptionSettingsCommand(SubscriptionId aggregateId) : base(aggregateId) { }
}
