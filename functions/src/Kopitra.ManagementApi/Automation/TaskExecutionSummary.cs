namespace Kopitra.ManagementApi.Automation;

public sealed record TaskExecutionSummary(
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? RunId,
    string? Message);
