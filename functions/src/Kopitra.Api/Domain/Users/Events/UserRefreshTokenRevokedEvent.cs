using EventFlow.Aggregates;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Domain.Users.Events;

public class UserRefreshTokenRevokedEvent : AggregateEvent<UserAggregate, UserId>
{
    public string SessionId { get; set; } = null!;
}
