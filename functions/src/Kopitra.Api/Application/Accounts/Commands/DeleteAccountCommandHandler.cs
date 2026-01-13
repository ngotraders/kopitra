using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class DeleteAccountCommandHandler : CommandHandler<AccountAggregate, AccountId, DeleteAccountCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, DeleteAccountCommand command, CancellationToken cancellationToken)
        {
            aggregate.Delete();
            return Task.CompletedTask;
        }
    }
}
