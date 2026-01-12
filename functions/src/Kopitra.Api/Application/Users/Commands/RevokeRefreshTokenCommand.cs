using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class RevokeRefreshTokenCommand : Command<UserAggregate, UserId>
{
    public string SessionId { get; set; } = null!;

    public RevokeRefreshTokenCommand(UserId aggregateId) : base(aggregateId) { }
}
