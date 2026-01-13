using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands
{
    /// <summary>
    /// Command to confirm account activation after user confirms code or enters info manually (Flow 2)
    /// </summary>
    public class ConfirmAccountActivationCommand : Command<AccountAggregate, AccountId>
    {
        public UserId UserId { get; set; } = null!;
        public BrokerType BrokerType { get; set; }

        public ConfirmAccountActivationCommand(AccountId accountId) : base(accountId) { }
    }
}
