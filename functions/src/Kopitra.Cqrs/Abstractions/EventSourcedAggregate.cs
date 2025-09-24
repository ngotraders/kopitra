using System;
using System.Collections.Generic;
using System.Linq;
using Kopitra.Cqrs.Events;

namespace Kopitra.Cqrs.Abstractions;

public abstract class EventSourcedAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public string Id { get; private set; } = string.Empty;

    public int Version { get; private set; } = -1;

    internal int OriginalVersion { get; private set; } = -1;

    protected void ApplyChange(IDomainEvent @event)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        When(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    protected void EnsureIdentity(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate id cannot be empty.", nameof(aggregateId));
        }

        if (string.IsNullOrEmpty(Id))
        {
            Id = aggregateId;
        }
        else if (!string.Equals(Id, aggregateId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Aggregate id mismatch. Current: '{Id}', attempted: '{aggregateId}'.");
        }
    }

    protected abstract void When(IDomainEvent @event);

    internal IReadOnlyList<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    internal void LoadFromHistory(string aggregateId, IEnumerable<EventEnvelope> history)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate id cannot be empty.", nameof(aggregateId));
        }

        Id = aggregateId;
        Version = -1;
        OriginalVersion = -1;

        foreach (var envelope in history.OrderBy(e => e.Version))
        {
            When(envelope.Payload);
            Version = envelope.Version;
        }

        OriginalVersion = Version;
        _uncommittedEvents.Clear();
    }

    internal void MarkChangesAsCommitted()
    {
        _uncommittedEvents.Clear();
        OriginalVersion = Version;
    }
}
