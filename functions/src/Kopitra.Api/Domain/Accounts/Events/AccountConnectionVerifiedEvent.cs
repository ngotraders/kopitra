using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Account connection verified event - fired when account connection is tested and verified
/// </summary>
public class AccountConnectionVerifiedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public bool IsConnected { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime VerifiedAt { get; set; }
}
