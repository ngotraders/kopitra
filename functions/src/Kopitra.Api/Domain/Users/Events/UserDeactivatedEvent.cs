using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User deactivated event
/// </summary>
public class UserDeactivatedEvent : AggregateEvent<UserAggregate, UserId>
{
    public string Reason { get; set; } = null!;
}
