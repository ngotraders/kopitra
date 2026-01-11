using EventFlow.Commands;
using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class CloseSignalCommandHandler : CommandHandler<SignalAggregate, SignalId, CloseSignalCommand>
{
    public override Task ExecuteAsync(SignalAggregate aggregate, CloseSignalCommand command, CancellationToken cancellationToken)
    {
        aggregate.Close(command.Reason);
        return Task.CompletedTask;
    }
}
