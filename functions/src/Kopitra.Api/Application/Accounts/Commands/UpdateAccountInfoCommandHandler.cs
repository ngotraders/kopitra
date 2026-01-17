using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{

    public class UpdateAccountInfoCommandHandler : CommandHandler<AccountAggregate, AccountId, UpdateAccountInfoCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, UpdateAccountInfoCommand command, CancellationToken cancellationToken)
        {
            aggregate.UpdateInfo(
                command.BrokerType,
                command.BrokerName,
                command.AccountNumber,
                command.ServerName);
            return Task.CompletedTask;
        }
    }
}
