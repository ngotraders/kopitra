using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class UpdateUserSettingsCommand : Command<UserAggregate, UserId>
{
    public Dictionary<string, object> Settings { get; set; } = null!;

    public UpdateUserSettingsCommand(UserId aggregateId) : base(aggregateId) { }
}
