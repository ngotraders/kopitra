using System;
using System.Collections.Generic;

namespace Kopitra.Cqrs.Events;

public interface IDomainEventEnvelope
{
    string EventId { get; }
    IDomainEvent Event { get; }
    int Version { get; }
    DateTimeOffset Timestamp { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}

public sealed class DomainEventEnvelope<TEvent> : IDomainEventEnvelope where TEvent : IDomainEvent
{
    public DomainEventEnvelope(string eventId, TEvent @event, int version, DateTimeOffset timestamp, IReadOnlyDictionary<string, string>? metadata = null)
    {
        EventId = eventId;
        Event = @event;
        Version = version;
        Timestamp = timestamp;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public string EventId { get; }

    public TEvent Event { get; }

    public int Version { get; }

    public DateTimeOffset Timestamp { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    IDomainEvent IDomainEventEnvelope.Event => Event;
}
