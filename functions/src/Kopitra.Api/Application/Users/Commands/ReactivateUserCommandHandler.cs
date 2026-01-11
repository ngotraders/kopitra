using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Handler for ReactivateUserCommand - admin only
/// </summary>
public class ReactivateUserCommandHandler : CommandHandler<UserAggregate, UserId, ReactivateUserCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (command.AdminUserId == null)
            throw new InvalidOperationException("Admin ID is required for reactivation.");

        aggregate.Reactivate();

        // Record admin action for audit trail
        aggregate.RecordAdminImpersonation(
            command.AdminUserId,
            "UserReactivated",
            new Dictionary<string, object> { { "memo", command.Memo ?? string.Empty } });

        return Task.CompletedTask;
    }
}
