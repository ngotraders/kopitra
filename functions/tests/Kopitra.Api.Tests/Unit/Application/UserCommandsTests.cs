using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class UserCommandsTests
{
    [TestMethod]
    public void RegisterUserCommand_CanBeInstantiated()
    {
        // Act
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var command = new RegisterUserCommand(userId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(userId, command.AggregateId);
    }

    [TestMethod]
    public void UpdateUserSettingsCommand_CanBeInstantiated()
    {
        // Act
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var command = new UpdateUserSettingsCommand(userId)
        {
            Settings = new Dictionary<string, object> { { "key", "value" } }
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(userId, command.AggregateId);
        Assert.AreEqual(1, command.Settings.Count);
    }
}

