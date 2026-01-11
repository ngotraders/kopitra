using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// Subscriber account created event
/// </summary>
public class UserSubscriberAccountCreatedEvent : AggregateEvent<UserAggregate, UserId>
{
    public AccountId AccountId { get; set; } = null!;
    public string BrokerAccountId { get; set; } = null!;
    public string Broker { get; set; } = null!;
}