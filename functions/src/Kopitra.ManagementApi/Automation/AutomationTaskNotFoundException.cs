namespace Kopitra.ManagementApi.Automation;

public sealed class AutomationTaskNotFoundException : Exception
{
    public string TaskId { get; }

    public AutomationTaskNotFoundException(string taskId)
        : base($"Automation task '{taskId}' was not found.")
    {
        TaskId = taskId;
    }
}
