using System;
using System.Collections.Generic;
using EventFlow.Aggregates;
using Kopitra.ManagementApi.Automation;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

public sealed class AutomationTaskAggregate : AggregateRoot<AutomationTaskAggregate, AutomationTaskAggregateId>,
    IEmit<AutomationTaskRunAccepted>
{
    private readonly List<TaskExecutionSummary> _executions = new();

    public AutomationTaskAggregate(AutomationTaskAggregateId id)
        : base(id)
    {
    }

    public string TenantId { get; private set; } = string.Empty;

    public string TaskId { get; private set; } = string.Empty;

    public IReadOnlyList<TaskExecutionSummary> Executions => _executions.AsReadOnly();

    public AutomationTaskRunExecutionResult RegisterRun(string tenantId, AutomationTask task, DateTimeOffset submittedAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(task);

        var runId = $"{submittedAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var response = new AutomationTaskRunResponse(task.TaskId, runId, "Accepted", submittedAt, "Task enqueued for execution.");
        var summary = new TaskExecutionSummary("Pending", submittedAt, null, runId, response.Message);

        Emit(new AutomationTaskRunAccepted(tenantId, task.TaskId, runId, submittedAt, summary.Status, summary.Message ?? string.Empty));

        return new AutomationTaskRunExecutionResult(summary, response);
    }

    public void Apply(AutomationTaskRunAccepted aggregateEvent)
    {
        TenantId = aggregateEvent.TenantId;
        TaskId = aggregateEvent.TaskId;

        _executions.Add(new TaskExecutionSummary(
            aggregateEvent.Status,
            aggregateEvent.SubmittedAt,
            null,
            aggregateEvent.RunId,
            aggregateEvent.Message));
    }

    public static AutomationTaskAggregateId BuildId(string tenantId, string taskId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new ArgumentException("Task id cannot be empty.", nameof(taskId));
        }

        return AutomationTaskAggregateId.With(tenantId, taskId);
    }
}
