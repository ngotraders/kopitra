using EventFlow.Commands;
using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Subscriptions.Commands;

public class ResumeSubscriptionCommandHandler : CommandHandler<SubscriptionAggregate, SubscriptionId, ResumeSubscriptionCommand>
{
    public override Task ExecuteAsync(
        SubscriptionAggregate aggregate,
        ResumeSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        aggregate.Resume();
        return Task.CompletedTask;
    }
}
