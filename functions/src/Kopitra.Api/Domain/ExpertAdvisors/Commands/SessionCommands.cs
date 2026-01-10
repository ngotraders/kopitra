using EventFlow.Commands;

namespace Kopitra.Api.Domain.ExpertAdvisors.Commands;

public class CreateSessionCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
}

public class AuthenticateSessionCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
}

public class RecordHeartbeatCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
}

public class CloseSessionCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
}
