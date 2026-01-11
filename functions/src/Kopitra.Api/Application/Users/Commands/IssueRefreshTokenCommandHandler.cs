using EventFlow.Commands;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class IssueRefreshTokenCommandHandler : CommandHandler<UserAggregate, UserId, IssueRefreshTokenCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, IssueRefreshTokenCommand command, CancellationToken cancellationToken)
    {
        aggregate.IssueRefreshToken(command.RefreshToken, command.ExpiresAt);
        return Task.CompletedTask;
    }
}
