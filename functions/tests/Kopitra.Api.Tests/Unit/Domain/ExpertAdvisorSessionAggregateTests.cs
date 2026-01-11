using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Tests.Unit.Domain;

[TestClass]
public class ExpertAdvisorSessionAggregateTests
{
    [TestMethod]
    public void ExpertAdvisorSessionAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var sessionId = new ExpertAdvisorSessionId($"expertadvisorsession-{Guid.NewGuid()}");
        var session = new ExpertAdvisorSessionAggregate(sessionId);

        // Assert
        Assert.IsNotNull(session);
        Assert.AreEqual(sessionId, session.Id);
    }
}

