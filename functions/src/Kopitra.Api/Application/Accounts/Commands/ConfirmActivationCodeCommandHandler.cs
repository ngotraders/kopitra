using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ConfirmActivationCodeCommandHandler : CommandHandler<ActivationCodeAggregate, ActivationCodeId, ConfirmActivationCodeCommand>
{
    public override Task ExecuteAsync(ActivationCodeAggregate aggregate, ConfirmActivationCodeCommand command, CancellationToken cancellationToken)
    {
        aggregate.Confirm(command.BrokerType);
        return Task.CompletedTask;
    }
}
