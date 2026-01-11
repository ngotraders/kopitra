using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// Provider role enabled event
/// </summary>
public class UserProviderRoleEnabledEvent : AggregateEvent<UserAggregate, UserId>
{
    public string ProviderDescription { get; set; } = null!;
}