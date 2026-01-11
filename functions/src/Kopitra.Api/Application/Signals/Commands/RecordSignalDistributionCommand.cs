using EventFlow.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class RecordSignalDistributionCommand : Command<SignalAggregate, SignalId>
{
    public int DistributedCount { get; set; }

    public RecordSignalDistributionCommand(SignalId aggregateId) : base(aggregateId) { }
}
