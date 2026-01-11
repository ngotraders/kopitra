using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Handler for ChangeUserPermissionsCommand - admin only
/// </summary>
public class ChangeUserPermissionsCommandHandler : CommandHandler<UserAggregate, UserId, ChangeUserPermissionsCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, ChangeUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        // Use new typed domain method which emits typed events and audit
        aggregate.ChangePermissions(
            command.CanProvide,
            command.CanSubscribe);

        return Task.CompletedTask;
    }
}
