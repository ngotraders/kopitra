using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when an EA initiates account activation (Flow 2)
/// The EA sends broker information which triggers activation code generation
/// </summary>
public class AccountActivationInitiatedEvent : AggregateEvent<AccountAggregate, AccountId>
{
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public DateTime InitiatedAt { get; set; }
}
