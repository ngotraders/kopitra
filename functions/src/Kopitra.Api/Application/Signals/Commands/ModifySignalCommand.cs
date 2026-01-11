using EventFlow.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands;

public class ModifySignalCommand : Command<SignalAggregate, SignalId>
{
    public PositionSize NewPositionSize { get; set; } = null!;
    public RiskManagement NewRiskManagement { get; set; } = null!;

    public ModifySignalCommand(SignalId aggregateId) : base(aggregateId) { }
}
