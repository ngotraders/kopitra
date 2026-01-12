using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class RevokeRefreshTokenCommandHandler : CommandHandler<UserAggregate, UserId, RevokeRefreshTokenCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, RevokeRefreshTokenCommand command, CancellationToken cancellationToken)
    {
        aggregate.RevokeRefreshToken(command.SessionId);
        return Task.CompletedTask;
    }
}
