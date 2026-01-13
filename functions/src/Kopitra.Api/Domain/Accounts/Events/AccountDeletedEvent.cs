using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account deleted event - fired when user deletes a trading account
/// </summary>
public class AccountDeletedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public DateTime DeletedAt { get; set; }
}
