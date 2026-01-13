using EventFlow.Aggregates;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when account activation is confirmed
/// User confirms activation code OR enters account info manually (Flow 2)
/// </summary>
public class AccountActivationConfirmedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public UserId UserId { get; set; } = null!;
    public BrokerType BrokerType { get; set; }
    public DateTime ConfirmedAt { get; set; }
}
