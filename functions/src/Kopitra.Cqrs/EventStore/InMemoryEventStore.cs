using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<string, List<EventEnvelope>> _streams = new(StringComparer.Ordinal);

    public Task<IReadOnlyCollection<EventEnvelope>> LoadAsync(string streamId, CancellationToken cancellationToken)
    {
        if (streamId is null)
        {
            throw new ArgumentNullException(nameof(streamId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_streams.TryGetValue(streamId, out var events))
        {
            lock (events)
            {
                return Task.FromResult<IReadOnlyCollection<EventEnvelope>>(events.ToArray());
            }
        }

        return Task.FromResult<IReadOnlyCollection<EventEnvelope>>(Array.Empty<EventEnvelope>());
    }

    public Task AppendAsync(string streamId, IReadOnlyCollection<EventEnvelope> events, int expectedVersion, CancellationToken cancellationToken)
    {
        if (streamId is null)
        {
            throw new ArgumentNullException(nameof(streamId));
        }

        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (events.Count == 0)
        {
            return Task.CompletedTask;
        }

        var stream = _streams.GetOrAdd(streamId, _ => new List<EventEnvelope>());
        lock (stream)
        {
            var currentVersion = stream.Count == 0 ? -1 : stream[^1].Version;
            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
            }

            foreach (var envelope in events)
            {
                if (envelope.Version != currentVersion + 1)
                {
                    throw new InvalidOperationException($"Event versions must be sequential. Expected {currentVersion + 1} but received {envelope.Version}.");
                }

                currentVersion = envelope.Version;
                stream.Add(envelope);
            }
        }

        return Task.CompletedTask;
    }
}
