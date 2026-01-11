using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class AuthenticateSessionCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
}
