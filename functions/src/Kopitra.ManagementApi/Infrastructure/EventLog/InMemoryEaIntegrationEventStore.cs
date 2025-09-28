using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Domain.Integration;

namespace Kopitra.ManagementApi.Infrastructure.EventLog;

public sealed class InMemoryEaIntegrationEventStore : IEaIntegrationEventStore
{
    private readonly ConcurrentDictionary<string, List<EaIntegrationEvent>> _events = new();

    public Task AppendAsync(EaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantEvents = _events.GetOrAdd(integrationEvent.TenantId, _ => new List<EaIntegrationEvent>());
        lock (tenantEvents)
        {
            tenantEvents.Add(integrationEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<EaIntegrationEvent>> ListAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_events.TryGetValue(tenantId, out var tenantEvents))
        {
            return Task.FromResult<IReadOnlyCollection<EaIntegrationEvent>>(Array.Empty<EaIntegrationEvent>());
        }

        lock (tenantEvents)
        {
            return Task.FromResult<IReadOnlyCollection<EaIntegrationEvent>>(tenantEvents.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.ReceivedAt).ToList());
        }
    }
}
