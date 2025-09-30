using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed class InMemoryAdminUserReadModelStore : IAdminUserReadModelStore
{
    private readonly ConcurrentDictionary<(string TenantId, string UserId), AdminUserReadModel> _store = new();

    public Task UpsertAsync(AdminUserReadModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[(model.TenantId, model.UserId)] = model;
        return Task.CompletedTask;
    }

    public Task<AdminUserReadModel?> GetAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue((tenantId, userId), out var model);
        return Task.FromResult(model);
    }

    public Task<IReadOnlyCollection<AdminUserReadModel>> ListAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = _store.Values.Where(v => v.TenantId == tenantId).OrderBy(v => v.DisplayName).ToList();
        return Task.FromResult<IReadOnlyCollection<AdminUserReadModel>>(results);
    }
}
