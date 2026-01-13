using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class UpdateAccountBalanceCommandHandler : CommandHandler<AccountAggregate, AccountId, UpdateAccountBalanceCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, UpdateAccountBalanceCommand command, CancellationToken cancellationToken)
        {
            aggregate.UpdateBalance(command.NewBalance);
            return Task.CompletedTask;
        }
    }
}
