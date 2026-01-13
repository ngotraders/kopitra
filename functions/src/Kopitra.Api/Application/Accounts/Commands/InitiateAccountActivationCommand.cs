using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    /// <summary>
    /// Command to initiate account activation (Flow 2 - EA sends broker info)
    /// </summary>
    public class InitiateAccountActivationCommand : Command<AccountAggregate, AccountId>
    {
        public string BrokerName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string ServerName { get; set; } = null!;

        public InitiateAccountActivationCommand(AccountId accountId) : base(accountId) { }
    }
}
