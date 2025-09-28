using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.Cqrs.Aggregates;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public sealed class AggregateRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, new()
{
    private readonly IEventStore _eventStore;
    private readonly IDomainEventPublisher _eventPublisher;

    public AggregateRepository(IEventStore eventStore, IDomainEventPublisher eventPublisher)
    {
        _eventStore = eventStore;
        _eventPublisher = eventPublisher;
    }

    public async Task<TAggregate> GetAsync(TId id, CancellationToken cancellationToken)
    {
        var aggregate = new TAggregate();
        aggregate.GetType().GetMethod("EnsureInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(aggregate, new object[] { id! });
        var streamId = GetStreamId(id);
        var history = await _eventStore.LoadAsync(streamId, cancellationToken).ConfigureAwait(false);
        aggregate.LoadFromHistory(history);
        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        var events = aggregate.GetUncommittedEvents().ToList();
        if (!events.Any())
        {
            return;
        }

        var expectedVersion = aggregate.PersistedVersion;
        var envelopes = new List<IDomainEventEnvelope>(events.Count);
        for (var index = 0; index < events.Count; index++)
        {
            var @event = events[index];
            var version = expectedVersion + index + 1;
            var envelopeType = typeof(DomainEventEnvelope<>).MakeGenericType(@event.GetType());
            var envelope = (IDomainEventEnvelope)Activator.CreateInstance(
                envelopeType,
                Guid.NewGuid().ToString("N"),
                @event,
                version,
                DateTimeOffset.UtcNow,
                null)!;
            envelopes.Add(envelope);
        }

        await _eventStore.AppendAsync(GetStreamId(aggregate.Id), expectedVersion, envelopes, cancellationToken).ConfigureAwait(false);
        aggregate.ClearUncommittedEvents();
        await _eventPublisher.PublishAsync(envelopes, cancellationToken).ConfigureAwait(false);
    }

    private static string GetStreamId(TId id)
    {
        return $"{typeof(TAggregate).Name}-{id}";
    }
}
