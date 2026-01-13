using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class VerifyAccountConnectionCommandHandler : CommandHandler<AccountAggregate, AccountId, VerifyAccountConnectionCommand>
    {
        public override Task ExecuteAsync(AccountAggregate aggregate, VerifyAccountConnectionCommand command, CancellationToken cancellationToken)
        {
            aggregate.VerifyConnection(command.IsConnected, command.CurrentBalance);
            return Task.CompletedTask;
        }
    }
}
