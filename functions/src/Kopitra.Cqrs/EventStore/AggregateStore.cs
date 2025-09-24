using System;
using System.Collections.Generic;
using System.Linq;
using Kopitra.Cqrs.Abstractions;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.EventStore;

public sealed class AggregateStore : IAggregateStore
{
    private readonly IEventStore _eventStore;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEventMetadataFactory _metadataFactory;

    public AggregateStore(IEventStore eventStore, IEventPublisher? eventPublisher = null, IEventMetadataFactory? metadataFactory = null)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _eventPublisher = eventPublisher ?? new NullEventPublisher();
        _metadataFactory = metadataFactory ?? new DefaultEventMetadataFactory();
    }

    public async Task<TAggregate> LoadAsync<TAggregate>(string aggregateId, CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate, new()
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate id cannot be empty.", nameof(aggregateId));
        }

        var streamId = BuildStreamId(typeof(TAggregate), aggregateId);
        var history = await _eventStore.LoadAsync(streamId, cancellationToken).ConfigureAwait(false);

        var aggregate = new TAggregate();
        aggregate.LoadFromHistory(aggregateId, history);
        return aggregate;
    }

    public async Task SaveAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken)
        where TAggregate : EventSourcedAggregate
    {
        if (aggregate is null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        if (string.IsNullOrWhiteSpace(aggregate.Id))
        {
            throw new InvalidOperationException($"Aggregate '{typeof(TAggregate).Name}' must have an identifier before persisting.");
        }

        var pending = aggregate.GetUncommittedEvents();
        if (pending.Count == 0)
        {
            return;
        }

        var pendingList = pending.ToList();
        var startVersion = aggregate.Version - pendingList.Count + 1;
        var aggregateType = typeof(TAggregate).Name;
        var streamId = BuildStreamId(typeof(TAggregate), aggregate.Id);

        var envelopes = new List<EventEnvelope>(pendingList.Count);
        for (var index = 0; index < pendingList.Count; index++)
        {
            var domainEvent = pendingList[index];
            var version = startVersion + index;
            var metadata = _metadataFactory.Create(domainEvent);
            var envelope = new EventEnvelope(
                aggregate.Id,
                aggregateType,
                version,
                domainEvent,
                DateTimeOffset.UtcNow,
                metadata);
            envelopes.Add(envelope);
        }

        await _eventStore.AppendAsync(streamId, envelopes, aggregate.OriginalVersion, cancellationToken).ConfigureAwait(false);

        foreach (var envelope in envelopes)
        {
            await _eventPublisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        }

        aggregate.MarkChangesAsCommitted();
    }

    private static string BuildStreamId(Type aggregateType, string aggregateId)
    {
        return $"{aggregateType.Name}-{aggregateId}";
    }
}
