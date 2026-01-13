using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    /// <summary>
    /// Handler for InitiateAccountActivationCommand
    /// </summary>
    public class InitiateAccountActivationCommandHandler : CommandHandler<AccountAggregate, AccountId, InitiateAccountActivationCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, InitiateAccountActivationCommand command, CancellationToken cancellationToken)
        {
            aggregate.InitiateActivation(command.BrokerName, command.AccountNumber, command.ServerName);
            return Task.CompletedTask;
        }
    }
}
