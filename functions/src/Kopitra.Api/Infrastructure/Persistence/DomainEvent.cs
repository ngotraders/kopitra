namespace Kopitra.Api.Infrastructure.Persistence;

/// <summary>
/// Domain event for event sourcing
/// </summary>
public class DomainEvent
{
    public int EventId { get; set; }
    public string AggregateId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }
}
