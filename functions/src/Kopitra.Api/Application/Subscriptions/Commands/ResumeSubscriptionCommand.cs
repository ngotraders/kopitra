using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class ResumeSubscriptionCommand : Command<SubscriptionAggregate, SubscriptionId>
{
    public ResumeSubscriptionCommand(SubscriptionId aggregateId) : base(aggregateId) { }
}
