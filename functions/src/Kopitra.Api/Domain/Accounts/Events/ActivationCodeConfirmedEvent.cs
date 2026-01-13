using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when an activation code is confirmed/redeemed
/// </summary>
public class ActivationCodeConfirmedEvent : AggregateEvent<ActivationCodeAggregate, ActivationCodeId>
{
    public string UserId { get; set; } = null!;
    public BrokerType BrokerType { get; set; }
    public DateTime ConfirmedAt { get; set; }
}
