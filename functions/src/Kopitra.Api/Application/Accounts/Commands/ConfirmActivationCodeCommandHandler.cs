using EventFlow.Commands;
using Kopitra.Api.Common;
using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Accounts.Commands;

public class ConfirmActivationCodeCommandHandler : CommandHandler<ActivationCodeAggregate, ActivationCodeId, ConfirmActivationCodeCommand>
{
    private readonly IClock _clock;

    public ConfirmActivationCodeCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public override Task ExecuteAsync(ActivationCodeAggregate aggregate, ConfirmActivationCodeCommand command, CancellationToken cancellationToken)
    {
        aggregate.Confirm(command.UserId, _clock.UtcNow.DateTime);
        return Task.CompletedTask;
    }
}