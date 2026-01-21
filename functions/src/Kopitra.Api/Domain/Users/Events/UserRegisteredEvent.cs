using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// User registered event - fired when user creates account
/// </summary>
public class UserRegisteredEvent : AggregateEvent<UserAggregate, UserId>
{
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
