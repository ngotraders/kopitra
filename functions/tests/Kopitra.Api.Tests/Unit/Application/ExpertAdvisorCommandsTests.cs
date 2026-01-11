using Kopitra.Api.Application.ExpertAdvisors.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class ExpertAdvisorCommandsTests
{
    [TestMethod]
    public void CreateSessionCommand_CanBeInstantiated()
    {
        // Act
        var sessionId = new ExpertAdvisorSessionId($"expertadvisorsession-{Guid.NewGuid()}");
        var command = new CreateSessionCommand(sessionId)
        {
            UserId = Guid.NewGuid().ToString(),
            AccountId = Guid.NewGuid().ToString()
        };

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(sessionId, command.AggregateId);
    }

    [TestMethod]
    public void AuthenticateSessionCommand_CanBeInstantiated()
    {
        // Act
        var sessionId = new ExpertAdvisorSessionId($"expertadvisorsession-{Guid.NewGuid()}");
        var command = new AuthenticateSessionCommand(sessionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(sessionId, command.AggregateId);
    }

    [TestMethod]
    public void RecordHeartbeatCommand_CanBeInstantiated()
    {
        // Act
        var sessionId = new ExpertAdvisorSessionId($"expertadvisorsession-{Guid.NewGuid()}");
        var command = new RecordHeartbeatCommand(sessionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(sessionId, command.AggregateId);
    }

    [TestMethod]
    public void CloseSessionCommand_CanBeInstantiated()
    {
        // Act
        var sessionId = new ExpertAdvisorSessionId($"expertadvisorsession-{Guid.NewGuid()}");
        var command = new CloseSessionCommand(sessionId);

        // Assert
        Assert.IsNotNull(command);
        Assert.AreEqual(sessionId, command.AggregateId);
    }
}

