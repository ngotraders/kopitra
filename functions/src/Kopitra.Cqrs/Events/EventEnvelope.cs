namespace Kopitra.Cqrs.Events;

public sealed record EventEnvelope(
    string AggregateId,
    string AggregateType,
    int Version,
    IDomainEvent Payload,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Metadata);
