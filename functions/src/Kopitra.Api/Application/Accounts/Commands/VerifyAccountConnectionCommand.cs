using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class VerifyAccountConnectionCommand : Command<AccountAggregate, AccountId>
    {
        public bool IsConnected { get; set; }
        public decimal CurrentBalance { get; set; }

        public VerifyAccountConnectionCommand(AccountId accountId) : base(accountId) { }
    }
}
