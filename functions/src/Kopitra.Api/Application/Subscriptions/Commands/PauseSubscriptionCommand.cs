using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class PauseSubscriptionCommand : Command<SubscriptionAggregate, SubscriptionId>
{
    public PauseSubscriptionCommand(SubscriptionId aggregateId) : base(aggregateId) { }
}
