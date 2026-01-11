using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Command to deactivate a user
/// Can be executed by admin only
/// </summary>
public class DeactivateUserCommand : Command<UserAggregate, UserId>
{
    public string Reason { get; set; } = null!;
    public UserId? AdminUserId { get; set; }

    public DeactivateUserCommand(UserId aggregateId) : base(aggregateId) { }
}
