using Kopitra.Api.Domain.Subscriptions;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain;

[TestClass]
public class SubscriptionAggregateTests
{
    [TestMethod]
    public void SubscriptionAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var subscription = new SubscriptionAggregate(subscriptionId);

        // Assert
        Assert.IsNotNull(subscription);
        Assert.AreEqual(subscriptionId, subscription.Id);
    }
}

