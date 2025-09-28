using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Queries;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;

public sealed record GetExpertAdvisorQuery(string TenantId, string ExpertAdvisorId) : IQuery<ExpertAdvisorReadModel?>;

public sealed class GetExpertAdvisorQueryHandler : IQueryHandler<GetExpertAdvisorQuery, ExpertAdvisorReadModel?>
{
    private readonly IExpertAdvisorReadModelStore _store;

    public GetExpertAdvisorQueryHandler(IExpertAdvisorReadModelStore store)
    {
        _store = store;
    }

    public Task<ExpertAdvisorReadModel?> HandleAsync(GetExpertAdvisorQuery query, CancellationToken cancellationToken)
    {
        return _store.GetAsync(query.TenantId, query.ExpertAdvisorId, cancellationToken);
    }
}
