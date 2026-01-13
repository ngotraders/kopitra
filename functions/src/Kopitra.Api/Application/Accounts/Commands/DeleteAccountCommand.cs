using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class DeleteAccountCommand : Command<AccountAggregate, AccountId>
    {
        public DeleteAccountCommand(AccountId accountId) : base(accountId) { }
    }
}
