using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account registered event - fired when user registers a new trading account (Flow 1)
/// </summary>
public class AccountRegisteredEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public UserId UserId { get; set; } = null!;
    public BrokerType BrokerType { get; set; }
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
}
