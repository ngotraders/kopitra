using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Domain.ValueObjects;
using Kopitra.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class CreateSignalCommandHandlerTests
{
    [TestMethod]
    public async Task Handler_Returns_Same_SignalId()
    {
        var handler = new CreateSignalCommandHandler();
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var cmd = new CreateSignalCommand(signalId)
        {
            ProviderId = new UserId($"user-{Guid.NewGuid()}"),
            Symbol = TradingSymbol.Create("EURUSD"),
            Action = OrderAction.Open,
            PositionSize = PositionSize.CreateFixedLot(1.0m),
            RiskManagement = new RiskManagement()
        };

        var result = await handler.HandleAsync(cmd);

        Assert.IsNotNull(result);
        Assert.AreEqual(signalId, result);
    }
}
