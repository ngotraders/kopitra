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
                command.AccountNumber,
                command.ServerName,
                command.ApiKey,
                command.ApiSecret);
            return Task.CompletedTask;
        }
    }
}
