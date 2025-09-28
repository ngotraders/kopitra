using System;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

[EventVersion("automation-task-run-accepted", 1)]
public sealed class AutomationTaskRunAccepted : AggregateEvent<AutomationTaskAggregate, AutomationTaskAggregateId>
{
    public AutomationTaskRunAccepted(
        string tenantId,
        string taskId,
        string runId,
        DateTimeOffset submittedAt,
        string status,
        string message)
    {
        TenantId = tenantId;
        TaskId = taskId;
        RunId = runId;
        SubmittedAt = submittedAt;
        Status = status;
        Message = message;
    }

    public string TenantId { get; }

    public string TaskId { get; }

    public string RunId { get; }

    public DateTimeOffset SubmittedAt { get; }

    public string Status { get; }

    public string Message { get; }
}
