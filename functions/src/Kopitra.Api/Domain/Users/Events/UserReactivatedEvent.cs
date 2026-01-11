using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User reactivated event - fired when admin reactivates a user
/// </summary>
public class UserReactivatedEvent : AggregateEvent<UserAggregate, UserId>
{
}
