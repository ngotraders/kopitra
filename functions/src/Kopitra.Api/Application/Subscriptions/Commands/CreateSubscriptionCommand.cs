using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class CreateSubscriptionCommand : Command<SubscriptionAggregate, SubscriptionId>
{
    public UserId SubscriberId { get; set; } = null!;
    public ProviderId ProviderId { get; set; } = null!;
    public AccountId SubscriberAccountId { get; set; } = null!;
    public FundingStrategy FundingStrategy { get; set; }
    public decimal MaxEquityRisk { get; set; }

    public CreateSubscriptionCommand(SubscriptionId aggregateId) : base(aggregateId) { }
}
