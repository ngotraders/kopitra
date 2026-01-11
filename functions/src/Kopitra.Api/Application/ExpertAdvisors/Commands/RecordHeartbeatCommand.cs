using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class RecordHeartbeatCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
}
