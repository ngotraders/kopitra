using Kopitra.Api.Application.Orders.Commands;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class OrderCommandsTests
{
    [TestMethod]
    public void CreateOrderCommand_CanBeInstantiated()
    {
        // Act
        var orderId = new OrderId($"order-{Guid.NewGuid()}");
        var command = new CreateOrderCommand(orderId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(orderId, command.AggregateId);
    }

    [TestMethod]
    public void ExecuteOrderCommand_CanBeInstantiated()
    {
        // Act
        var orderId = new OrderId($"order-{Guid.NewGuid()}");
        var command = new ExecuteOrderCommand(orderId)
        {
            ExecutedLot = 1.0m,
            ExecutionPrice = 1.0950m
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(orderId, command.AggregateId);
        Assert.AreEqual(1.0m, command.ExecutedLot);
    }

    [TestMethod]
    public void CancelOrderCommand_CanBeInstantiated()
    {
        // Act
        var orderId = new OrderId($"order-{Guid.NewGuid()}");
        var command = new CancelOrderCommand(orderId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(orderId, command.AggregateId);
    }

    [TestMethod]
    public void ModifyOrderCommand_CanBeInstantiated()
    {
        // Act
        var orderId = new OrderId($"order-{Guid.NewGuid()}");
        var command = new ModifyOrderCommand(orderId)
        {
            NewLot = 2.0m,
            NewRiskManagement = new RiskManagement(0.03m)
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(orderId, command.AggregateId);
        Assert.AreEqual(2.0m, command.NewLot);
    }
}
