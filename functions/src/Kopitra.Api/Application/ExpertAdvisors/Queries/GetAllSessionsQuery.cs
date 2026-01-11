using EventFlow.Queries;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Queries;

public class GetAllSessionsQuery : IQuery<IReadOnlyCollection<ExpertAdvisorSessionReadModel>>
{
}
