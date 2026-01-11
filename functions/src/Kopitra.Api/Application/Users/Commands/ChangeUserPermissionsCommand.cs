using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

/// <summary>
/// Command to change user permissions (provider/subscriber rights)
/// Can only be executed by admin
/// </summary>
public class ChangeUserPermissionsCommand : Command<UserAggregate, UserId>
{
    public bool? CanProvide { get; set; }
    public bool? CanSubscribe { get; set; }
 
    public ChangeUserPermissionsCommand(UserId aggregateId) : base(aggregateId) { }
}
