using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account balance updated event - fired when account balance is synced from EA
/// </summary>
public class AccountBalanceUpdatedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public decimal Balance { get; set; }
    public DateTime SyncedAt { get; set; }
}
