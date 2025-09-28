using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
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
        AutomationTaskRunExecutionResult? executionResult = null;

        await _aggregateStore.UpdateAsync<AutomationTaskAggregate, AutomationTaskAggregateId>(
            aggregateId,
            SourceId.New,
            (aggregate, token) =>
            {
                executionResult = aggregate.RegisterRun(tenantId, task, submittedAt);
                return Task.FromResult<IExecutionResult>(executionResult);
            },
            cancellationToken);

        ArgumentNullException.ThrowIfNull(executionResult);

        await _repository.UpdateAutomationTaskExecutionAsync(tenantId, taskId, executionResult.Summary, cancellationToken);

        return executionResult.Response;
    }
}
