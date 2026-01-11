using EventFlow.Commands;
using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class RecordSignalDistributionCommandHandler : CommandHandler<SignalAggregate, SignalId, RecordSignalDistributionCommand>
{
    public override Task ExecuteAsync(SignalAggregate aggregate, RecordSignalDistributionCommand command, CancellationToken cancellationToken)
    {
        aggregate.RecordDistribution(command.DistributedCount);
        return Task.CompletedTask;
    }
}
