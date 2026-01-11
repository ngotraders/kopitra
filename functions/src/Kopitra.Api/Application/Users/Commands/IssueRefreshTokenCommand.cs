using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class IssueRefreshTokenCommand : Command<UserAggregate, UserId>
{
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    public IssueRefreshTokenCommand(UserId aggregateId) : base(aggregateId) { }
}
