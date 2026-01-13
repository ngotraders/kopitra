using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account information updated event - fired when account details are modified
/// </summary>
public class AccountInfoUpdatedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public string? AccountNumber { get; set; }
    public string? ServerName { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
}
