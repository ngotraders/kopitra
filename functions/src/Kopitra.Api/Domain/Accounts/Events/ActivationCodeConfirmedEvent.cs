using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when an activation code is confirmed/redeemed
/// </summary>
public class ActivationCodeConfirmedEvent : AggregateEvent<ActivationCodeAggregate, ActivationCodeId>
{
    public UserId UserId { get; set; } = null!;
    public DateTimeOffset ConfirmedAt { get; set; }
}
