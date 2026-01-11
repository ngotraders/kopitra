using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Command to reactivate a deactivated user
/// Can only be executed by admin
/// </summary>
public class ReactivateUserCommand : Command<UserAggregate, UserId>
{
    public UserId AdminUserId { get; set; } = null!;
    public string? Memo { get; set; }

    public ReactivateUserCommand(UserId aggregateId) : base(aggregateId) { }
}
