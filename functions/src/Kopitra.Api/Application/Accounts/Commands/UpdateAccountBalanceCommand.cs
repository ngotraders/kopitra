using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class UpdateAccountBalanceCommand : Command<AccountAggregate, AccountId>
    {
        public decimal NewBalance { get; set; }

        public UpdateAccountBalanceCommand(AccountId accountId) : base(accountId) { }
    }
}
