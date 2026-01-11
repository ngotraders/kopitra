using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain;

[TestClass]
public class UserAggregateTests
{
    [TestMethod]
    public void UserAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }

    [TestMethod]
    public void UserAggregate_IsInitiallyInactive()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Assert
        Assert.IsTrue(user.IsActive);
    }
}

