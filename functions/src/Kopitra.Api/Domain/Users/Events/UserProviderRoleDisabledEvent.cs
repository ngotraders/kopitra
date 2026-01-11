using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// Provider role disabled event
/// </summary>
public class UserProviderRoleDisabledEvent : AggregateEvent<UserAggregate, UserId>
{
}