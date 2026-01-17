using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class UpdateAccountInfoCommand : Command<AccountAggregate, AccountId>
    {
        public BrokerType? BrokerType { get; set; }
        public string? BrokerName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ServerName { get; set; }

        public UpdateAccountInfoCommand(AccountId accountId) : base(accountId) { }
    }
}
