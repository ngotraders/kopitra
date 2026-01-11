using EventFlow.Commands;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Commands;

public class CreateSessionCommand(ExpertAdvisorSessionId aggregateId) : Command<ExpertAdvisorSessionAggregate, ExpertAdvisorSessionId>(aggregateId)
{
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
}
