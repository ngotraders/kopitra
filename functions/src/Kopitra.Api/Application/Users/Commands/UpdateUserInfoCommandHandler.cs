using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Handler for UpdateUserInfoCommand
/// </summary>
public class UpdateUserInfoCommandHandler : CommandHandler<UserAggregate, UserId, UpdateUserInfoCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, UpdateUserInfoCommand command, CancellationToken cancellationToken)
    {
        aggregate.UpdateInfo(command.Email, command.DisplayName);
        return Task.CompletedTask;
    }
}
