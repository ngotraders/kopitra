using EventFlow.Queries;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Queries;

public class GetSessionByIdQuery : IQuery<ExpertAdvisorSessionReadModel>
{
    public string SessionId { get; }

    public GetSessionByIdQuery(string sessionId)
    {
        SessionId = sessionId;
    }
}
