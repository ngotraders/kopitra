using Kopitra.Cqrs.EventStore;
using Kopitra.ManagementApi.Automation.EventSourcing;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Automation;

public sealed class AutomationTaskService : IAutomationTaskService
{
    private readonly IManagementRepository _repository;
    private readonly IClock _clock;
    private readonly IAggregateStore _aggregateStore;

    public AutomationTaskService(IManagementRepository repository, IClock clock, IAggregateStore aggregateStore)
    {
        _repository = repository;
        _clock = clock;
        _aggregateStore = aggregateStore;
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
        var aggregateId = AutomationTaskAggregate.BuildId(tenantId, taskId);
        var aggregate = await _aggregateStore.LoadAsync<AutomationTaskAggregate>(aggregateId, cancellationToken);
        var (summary, response) = aggregate.RegisterRun(tenantId, task, submittedAt);

        await _repository.UpdateAutomationTaskExecutionAsync(tenantId, taskId, summary, cancellationToken);
        await _aggregateStore.SaveAsync(aggregate, cancellationToken);

        return response;
    }
}
