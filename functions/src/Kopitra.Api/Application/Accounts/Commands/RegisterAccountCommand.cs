using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    public class RegisterAccountCommand : Command<AccountAggregate, AccountId>
    {
        public UserId UserId { get; set; } = null!;
        public BrokerType BrokerType { get; set; }
        public string BrokerName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string ServerName { get; set; } = null!;

        public RegisterAccountCommand(AccountId accountId) : base(accountId) { }
    }
}
