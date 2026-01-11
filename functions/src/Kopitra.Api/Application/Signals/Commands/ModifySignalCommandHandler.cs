using EventFlow.Commands;
using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class ModifySignalCommandHandler : CommandHandler<SignalAggregate, SignalId, ModifySignalCommand>
{
    public override Task ExecuteAsync(SignalAggregate aggregate, ModifySignalCommand command, CancellationToken cancellationToken)
    {
        aggregate.Modify(command.NewPositionSize, command.NewRiskManagement);
        return Task.CompletedTask;
    }
}
