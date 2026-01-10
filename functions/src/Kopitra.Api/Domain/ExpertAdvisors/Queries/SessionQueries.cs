using EventFlow.Queries;

namespace Kopitra.Api.Domain.ExpertAdvisors.Queries;

public class GetSessionByIdQuery : IQuery<ExpertAdvisorSessionReadModel>
{
    public string SessionId { get; }

    public GetSessionByIdQuery(string sessionId)
    {
        SessionId = sessionId;
    }
}

public class GetAllSessionsQuery : IQuery<IReadOnlyCollection<ExpertAdvisorSessionReadModel>>
{
}
