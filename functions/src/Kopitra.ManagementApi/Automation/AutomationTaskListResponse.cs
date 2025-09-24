namespace Kopitra.ManagementApi.Automation;

public sealed record AutomationTaskListResponse(int Count, IReadOnlyList<AutomationTask> Items);
