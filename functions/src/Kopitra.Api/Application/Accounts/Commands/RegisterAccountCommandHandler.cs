using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class RegisterAccountCommandHandler : CommandHandler<AccountAggregate, AccountId, RegisterAccountCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, RegisterAccountCommand command, CancellationToken cancellationToken)
        {
            aggregate.Register(
                command.UserId,
                command.BrokerType,
                command.BrokerName,
                command.AccountNumber,
                command.ServerName);
            return Task.CompletedTask;
        }
    }
}
