using Kopitra.Api.Application.Subscriptions.Commands;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class SubscriptionCommandsTests
{
    [TestMethod]
    public void CreateSubscriptionCommand_CanBeInstantiated()
    {
        // Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var command = new CreateSubscriptionCommand(subscriptionId)
        {
            SubscriberId = new UserId($"user-{Guid.NewGuid()}"),
            ProviderId = new ProviderId($"provider-{Guid.NewGuid()}"),
            SubscriberAccountId = new AccountId($"account-{Guid.NewGuid()}"),
            FundingStrategy = FundingStrategy.FixedLot,
            MaxEquityRisk = 0.02m
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(subscriptionId, command.AggregateId);
        Assert.IsNotNull(command.SubscriberId);
    }

    [TestMethod]
    public void PauseSubscriptionCommand_CanBeInstantiated()
    {
        // Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var command = new PauseSubscriptionCommand(subscriptionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(subscriptionId, command.AggregateId);
    }

    [TestMethod]
    public void ResumeSubscriptionCommand_CanBeInstantiated()
    {
        // Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var command = new ResumeSubscriptionCommand(subscriptionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(subscriptionId, command.AggregateId);
    }

    [TestMethod]
    public void CancelSubscriptionCommand_CanBeInstantiated()
    {
        // Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var command = new CancelSubscriptionCommand(subscriptionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(subscriptionId, command.AggregateId);
    }

    [TestMethod]
    public void UpdateSubscriptionSettingsCommand_CanBeInstantiated()
    {
        // Act
        var subscriptionId = new SubscriptionId($"subscription-{Guid.NewGuid()}");
        var command = new UpdateSubscriptionSettingsCommand(subscriptionId)
        {
            NewFundingStrategy = FundingStrategy.Proportional,
            NewMaxEquityRisk = 0.03m
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(subscriptionId, command.AggregateId);
        Assert.AreEqual(FundingStrategy.Proportional, command.NewFundingStrategy);
    }
}

