using EventFlow.Core;

namespace Kopitra.Api.Domain.ExpertAdvisors;

public class ExpertAdvisorSessionId : Identity<ExpertAdvisorSessionId>
{
    public ExpertAdvisorSessionId(string value) : base(value)
    {
    }
}
