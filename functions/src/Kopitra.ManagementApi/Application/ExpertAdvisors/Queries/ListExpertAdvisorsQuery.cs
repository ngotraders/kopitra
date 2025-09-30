using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Infrastructure.ReadModels;

namespace Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;

public sealed record ListExpertAdvisorsQuery(string TenantId) : IQuery<IReadOnlyCollection<ExpertAdvisorReadModel>>;

public sealed class ListExpertAdvisorsQueryHandler : IQueryHandler<ListExpertAdvisorsQuery, IReadOnlyCollection<ExpertAdvisorReadModel>>
{
    private readonly IExpertAdvisorReadModelStore _store;

    public ListExpertAdvisorsQueryHandler(IExpertAdvisorReadModelStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<ExpertAdvisorReadModel>> HandleAsync(ListExpertAdvisorsQuery query, CancellationToken cancellationToken)
    {
        return _store.ListAsync(query.TenantId, cancellationToken);
    }
}
