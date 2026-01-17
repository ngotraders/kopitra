using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class IssueRefreshTokenCommand : Command<UserAggregate, UserId>
{
    public string SessionId { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }

    public IssueRefreshTokenCommand(UserId aggregateId) : base(aggregateId) { }
}
