using Kopitra.Api.Domain.Signals;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain;

[TestClass]
public class SignalAggregateTests
{
    [TestMethod]
    public void SignalAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var signal = new SignalAggregate(signalId);

        // Assert
        Assert.IsNotNull(signal);
        Assert.AreEqual(signalId, signal.Id);
    }

    [TestMethod]
    public void SignalAggregate_IsNotClosedByDefault()
    {
        // Arrange
        var signalId = new SignalId($"signal-{Guid.NewGuid()}");
        var signal = new SignalAggregate(signalId);

        // Assert
        Assert.IsFalse(signal.IsClosed);
    }
}

