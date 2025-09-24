namespace Kopitra.ManagementApi.Automation;

public sealed record AutomationTaskRunResponse(
    string TaskId,
    string RunId,
    string Status,
    DateTimeOffset SubmittedAt,
    string Message);
