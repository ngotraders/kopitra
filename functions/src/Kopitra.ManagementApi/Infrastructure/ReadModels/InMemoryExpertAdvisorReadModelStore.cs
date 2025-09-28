using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed class InMemoryExpertAdvisorReadModelStore : IExpertAdvisorReadModelStore
{
    private readonly ConcurrentDictionary<(string TenantId, string ExpertAdvisorId), ExpertAdvisorReadModel> _store = new();

    public Task UpsertAsync(ExpertAdvisorReadModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[(model.TenantId, model.ExpertAdvisorId)] = model;
        return Task.CompletedTask;
    }

    public Task<ExpertAdvisorReadModel?> GetAsync(string tenantId, string expertAdvisorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue((tenantId, expertAdvisorId), out var model);
        return Task.FromResult(model);
    }

    public Task<IReadOnlyCollection<ExpertAdvisorReadModel>> ListAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = _store.Values.Where(v => v.TenantId == tenantId).OrderBy(v => v.DisplayName).ToList();
        return Task.FromResult<IReadOnlyCollection<ExpertAdvisorReadModel>>(items);
    }
}
