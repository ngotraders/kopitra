using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class CancelSubscriptionCommand : Command<SubscriptionAggregate, SubscriptionId>
{
    public string Reason { get; set; } = null!;

    public CancelSubscriptionCommand(SubscriptionId aggregateId) : base(aggregateId) { }
}
