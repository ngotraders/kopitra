using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User login event - fired for audit trail
/// </summary>
public class UserLoginEvent : AggregateEvent<UserAggregate, UserId>
{
}
