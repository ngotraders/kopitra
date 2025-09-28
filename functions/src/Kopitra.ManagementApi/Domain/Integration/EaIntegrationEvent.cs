namespace Kopitra.ManagementApi.Domain.Integration;

public sealed record EaIntegrationEvent(
    string TenantId,
    string Source,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt);
