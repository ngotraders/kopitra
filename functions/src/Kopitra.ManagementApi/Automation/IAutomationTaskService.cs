namespace Kopitra.ManagementApi.Automation;

public interface IAutomationTaskService
{
    Task<IReadOnlyList<AutomationTask>> ListTasksAsync(string tenantId, CancellationToken cancellationToken);

    Task<AutomationTask?> GetTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken);

    Task<AutomationTaskRunResponse> RunTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken);
}
