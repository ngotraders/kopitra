using System;
using System.Collections.Generic;
using Kopitra.Cqrs.Abstractions;
using Kopitra.Cqrs.Events;
using Kopitra.ManagementApi.Automation;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

public sealed class AutomationTaskAggregate : EventSourcedAggregate
{
    private readonly List<TaskExecutionSummary> _executions = new();

    public string TenantId { get; private set; } = string.Empty;

    public string TaskId { get; private set; } = string.Empty;

    public IReadOnlyList<TaskExecutionSummary> Executions => _executions.AsReadOnly();

    public (TaskExecutionSummary Summary, AutomationTaskRunResponse Response) RegisterRun(string tenantId, AutomationTask task, DateTimeOffset submittedAt)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(task);

        var aggregateId = BuildId(tenantId, task.TaskId);
        EnsureIdentity(aggregateId);

        var runId = $"{submittedAt:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var response = new AutomationTaskRunResponse(task.TaskId, runId, "Accepted", submittedAt, "Task enqueued for execution.");
        var summary = new TaskExecutionSummary("Pending", submittedAt, null, runId, response.Message);

        ApplyChange(new AutomationTaskRunAccepted(tenantId, task.TaskId, runId, submittedAt, summary.Status, summary.Message ?? string.Empty));

        return (summary, response);
    }

    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case AutomationTaskRunAccepted accepted:
                TenantId = accepted.TenantId;
                TaskId = accepted.TaskId;
                EnsureIdentity(BuildId(accepted.TenantId, accepted.TaskId));
                _executions.Add(new TaskExecutionSummary(
                    accepted.Status,
                    accepted.SubmittedAt,
                    null,
                    accepted.RunId,
                    accepted.Message));
                break;
        }
    }

    public static string BuildId(string tenantId, string taskId)
    {
        return $"{tenantId}:{taskId}";
    }
}
