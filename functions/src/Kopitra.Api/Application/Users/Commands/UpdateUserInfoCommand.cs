using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Command to update user's basic information (email, display name)
/// Can be used by user themselves or by admin
/// </summary>
public class UpdateUserInfoCommand : Command<UserAggregate, UserId>
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }

    public UpdateUserInfoCommand(UserId aggregateId) : base(aggregateId) { }
}
