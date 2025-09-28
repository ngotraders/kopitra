using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed class InMemoryCopyTradeGroupReadModelStore : ICopyTradeGroupReadModelStore
{
    private readonly ConcurrentDictionary<(string TenantId, string GroupId), CopyTradeGroupReadModel> _store = new();

    public Task UpsertAsync(CopyTradeGroupReadModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store[(model.TenantId, model.GroupId)] = model;
        return Task.CompletedTask;
    }

    public Task<CopyTradeGroupReadModel?> GetAsync(string tenantId, string groupId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryGetValue((tenantId, groupId), out var model);
        if (model is null)
        {
            return Task.FromResult<CopyTradeGroupReadModel?>(null);
        }

        var copy = model with { Members = model.Members.OrderBy(m => m.MemberId).ToArray() };
        return Task.FromResult<CopyTradeGroupReadModel?>(copy);
    }
}
