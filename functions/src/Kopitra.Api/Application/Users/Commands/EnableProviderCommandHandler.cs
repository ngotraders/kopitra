using EventFlow.Commands;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Commands;

public class EnableProviderCommandHandler : CommandHandler<UserAggregate, UserId, EnableProviderCommand>
{
    public override Task ExecuteAsync(UserAggregate aggregate, EnableProviderCommand command, CancellationToken cancellationToken)
    {
        aggregate.EnableProvider(command.ProviderDescription);
        return Task.CompletedTask;
    }
}
