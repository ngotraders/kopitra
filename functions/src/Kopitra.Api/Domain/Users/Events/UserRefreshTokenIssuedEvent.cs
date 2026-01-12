using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

/// <summary>
/// Refresh token issued for user
/// </summary>
public class UserRefreshTokenIssuedEvent : AggregateEvent<UserAggregate, UserId>
{
    public string RefreshToken { get; set; } = null!;
    public string SessionId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
