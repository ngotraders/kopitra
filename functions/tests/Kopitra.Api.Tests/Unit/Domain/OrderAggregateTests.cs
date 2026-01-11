using Kopitra.Api.Domain.Orders;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain;

[TestClass]
public class OrderAggregateTests
{
    [TestMethod]
    public void OrderAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var orderId = new OrderId($"order-{Guid.NewGuid()}");
        var order = new OrderAggregate(orderId);

        // Assert
        Assert.IsNotNull(order);
        Assert.AreEqual(orderId, order.Id);
    }
}

