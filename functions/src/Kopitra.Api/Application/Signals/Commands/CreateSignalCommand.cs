using EventFlow.Commands;
using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Signals.Commands
{
    public class CreateSignalCommand : Command<SignalAggregate, SignalId>
    {
        public UserId ProviderId { get; set; } = null!;
        public TradingSymbol Symbol { get; set; } = null!;
        public OrderAction Action { get; set; }
        public PositionSize PositionSize { get; set; } = null!;
        public RiskManagement RiskManagement { get; set; } = null!;

        public CreateSignalCommand(SignalId aggregateId) : base(aggregateId) { }
    }
}
