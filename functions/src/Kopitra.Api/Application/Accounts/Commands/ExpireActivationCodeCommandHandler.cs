using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ExpireActivationCodeCommandHandler : CommandHandler<ActivationCodeAggregate, ActivationCodeId, ExpireActivationCodeCommand>
{
    public override Task ExecuteAsync(ActivationCodeAggregate aggregate, ExpireActivationCodeCommand command, CancellationToken cancellationToken)
    {
        aggregate.Expire();
        return Task.CompletedTask;
    }
}
