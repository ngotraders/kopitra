using EventFlow.Commands;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class GenerateActivationCodeCommandHandler : CommandHandler<ActivationCodeAggregate, ActivationCodeId, GenerateActivationCodeCommand>
{
    public override Task ExecuteAsync(ActivationCodeAggregate aggregate, GenerateActivationCodeCommand command, CancellationToken cancellationToken)
    {
        aggregate.Generate(
            command.Code,
            command.BrokerName,
            command.AccountNumber,
            command.ServerName,
            command.UserId,
            command.ExpiresAt);
        return Task.CompletedTask;
    }
}
