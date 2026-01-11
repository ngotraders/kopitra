using EventFlow.Commands;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class UpdateUserSettingsCommandHandler : CommandHandler<UserAggregate, UserId, UpdateUserSettingsCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, UpdateUserSettingsCommand command, CancellationToken cancellationToken)
    {
        aggregate.UpdateSettings(command.Settings);
        return Task.CompletedTask;
    }
}
