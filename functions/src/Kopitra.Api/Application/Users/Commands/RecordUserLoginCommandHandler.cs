using EventFlow.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class RecordUserLoginCommandHandler : CommandHandler<UserAggregate, UserId, RecordUserLoginCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, RecordUserLoginCommand command, CancellationToken cancellationToken)
    {
        aggregate.RecordLogin();
        return Task.CompletedTask;
    }
}
