using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    /// <summary>
    /// Handler for ConfirmAccountActivationCommand
    /// </summary>
    public class ConfirmAccountActivationCommandHandler : CommandHandler<AccountAggregate, AccountId, ConfirmAccountActivationCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, ConfirmAccountActivationCommand command, CancellationToken cancellationToken)
        {
            aggregate.ConfirmActivation(command.UserId, command.BrokerType);
            return Task.CompletedTask;
        }
    }
}
