using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User information updated event - for admin direct change or user self-update
/// </summary>
public class UserInfoUpdatedEvent : AggregateEvent<UserAggregate, UserId>
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}
