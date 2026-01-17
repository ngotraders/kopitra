using EventFlow.Commands;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class GenerateActivationCodeCommandHandler : CommandHandler<ActivationCodeAggregate, ActivationCodeId, GenerateActivationCodeCommand>
{
    private readonly IClock _clock;

    public GenerateActivationCodeCommandHandler(IClock clock)
    {
        _clock = clock;
    }
    public override Task ExecuteAsync(ActivationCodeAggregate aggregate, GenerateActivationCodeCommand command, CancellationToken cancellationToken)
    {

        var expiresAt = _clock.UtcNow.AddHours(24); // Default 24 hours
        aggregate.Generate(
            command.Code,
            command.BrokerType,
            command.BrokerName,
            command.AccountNumber,
            command.ServerName,
            command.UserId,
            expiresAt);
        return Task.CompletedTask;
    }
}