using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Automation;

public sealed class AutomationTaskService : IAutomationTaskService
{
    private readonly IManagementRepository _repository;
    private readonly IClock _clock;

    public AutomationTaskService(IManagementRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task<IReadOnlyList<AutomationTask>> ListTasksAsync(string tenantId, CancellationToken cancellationToken)
    {
        return _repository.ListAutomationTasksAsync(tenantId, cancellationToken);
    }

    public Task<AutomationTask?> GetTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken)
    {
        return _repository.FindAutomationTaskAsync(tenantId, taskId, cancellationToken);
    }

    public async Task<AutomationTaskRunResponse> RunTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken)
    {
        var task = await _repository.FindAutomationTaskAsync(tenantId, taskId, cancellationToken);
        if (task is null)
        {
            throw new AutomationTaskNotFoundException(taskId);
        }

        var submittedAt = _clock.UtcNow;
        var runId = $"{submittedAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";

        var summary = new TaskExecutionSummary(
            Status: "Pending",
            StartedAt: submittedAt,
            CompletedAt: null,
            RunId: runId,
            Message: "Task enqueued for execution.");

        await _repository.UpdateAutomationTaskExecutionAsync(tenantId, taskId, summary, cancellationToken);

        return new AutomationTaskRunResponse(task.TaskId, runId, "Accepted", submittedAt, "Task enqueued for execution.");
    }
}
