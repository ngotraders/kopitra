using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Handler for DeactivateUserCommand - admin only
/// </summary>
public class DeactivateUserCommandHandler : CommandHandler<UserAggregate, UserId, DeactivateUserCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == null)
            throw new InvalidOperationException("Admin ID is required for deactivation.");

        aggregate.Deactivate(command.Reason);

        // Record admin action for audit trail
        aggregate.RecordAdminImpersonation(
            command.AdminUserId,
            "UserDeactivated",
            new Dictionary<string, object> { { "reason", command.Reason } });

        return Task.CompletedTask;
    }
}
