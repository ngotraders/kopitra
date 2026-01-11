using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class SignalCommandsTests
{
    [TestMethod]
    public void CreateSignalCommand_CanBeInstantiated()
    {
        // Act
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var command = new CreateSignalCommand(signalId)
        {
            ProviderId = new UserId($"user-{Guid.NewGuid()}"),
            Symbol = new TradingSymbol("EURUSD"),
            Action = OrderAction.Open,
            PositionSize = new PositionSize(1.0m),
            RiskManagement = new RiskManagement(0.02m)
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(signalId, command.AggregateId);
        Assert.IsNotNull(command.ProviderId);
    }

    [TestMethod]
    public void ModifySignalCommand_CanBeInstantiated()
    {
        // Act
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var command = new ModifySignalCommand(signalId)
        {
            NewPositionSize = new PositionSize(2.0m),
            NewRiskManagement = new RiskManagement(0.03m)
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(signalId, command.AggregateId);
    }

    [TestMethod]
    public void CloseSignalCommand_CanBeInstantiated()
    {
        // Act
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var command = new CloseSignalCommand(signalId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(signalId, command.AggregateId);
    }

    [TestMethod]
    public void RecordSignalDistributionCommand_CanBeInstantiated()
    {
        // Act
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var command = new RecordSignalDistributionCommand(signalId)
        {
            DistributedCount = 5
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(signalId, command.AggregateId);
        Assert.AreEqual(5, command.DistributedCount);
    }
}

