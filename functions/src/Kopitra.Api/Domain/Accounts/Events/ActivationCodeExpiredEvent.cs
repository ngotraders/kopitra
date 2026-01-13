using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Accounts.Events;

/// <summary>
/// Event fired when an activation code expires
/// </summary>
public class ActivationCodeExpiredEvent : AggregateEvent<ActivationCodeAggregate, ActivationCodeId>
{
    public DateTime ExpiredAt { get; set; }
}
