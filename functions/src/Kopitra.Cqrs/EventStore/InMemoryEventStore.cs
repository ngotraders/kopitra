using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEventEnvelope>> _streams = new();

    public Task<IReadOnlyCollection<IDomainEventEnvelope>> LoadAsync(string streamId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_streams.TryGetValue(streamId, out var events))
        {
            return Task.FromResult<IReadOnlyCollection<IDomainEventEnvelope>>(events.ToList());
        }

        return Task.FromResult<IReadOnlyCollection<IDomainEventEnvelope>>(Array.Empty<IDomainEventEnvelope>());
    }

    public Task AppendAsync(string streamId, int expectedVersion, IReadOnlyCollection<IDomainEventEnvelope> events, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = _streams.GetOrAdd(streamId, _ => new List<IDomainEventEnvelope>());
        lock (stream)
        {
            var currentVersion = stream.Count == 0 ? -1 : stream[^1].Version;
            if (currentVersion != expectedVersion)
            {
                throw new InvalidOperationException($"Concurrency conflict for stream {streamId}. Expected version {expectedVersion} but found {currentVersion}.");
            }

            stream.AddRange(events);
        }

        return Task.CompletedTask;
    }
}
