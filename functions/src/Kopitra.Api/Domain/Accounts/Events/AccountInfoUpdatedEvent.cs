using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account information updated event - fired when account details are modified
/// </summary>
public class AccountInfoUpdatedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public BrokerType? BrokerType { get; set; }
    public string? BrokerName { get; set; }
    public string? AccountNumber { get; set; }
    public string? ServerName { get; set; }
}
