using EventFlow.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class CloseSignalCommand : Command<SignalAggregate, SignalId>
{
    public string Reason { get; set; } = null!;

    public CloseSignalCommand(SignalId aggregateId) : base(aggregateId) { }
}
