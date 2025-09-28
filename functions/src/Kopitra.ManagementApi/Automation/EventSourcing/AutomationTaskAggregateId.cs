using System;
using EventFlow.Core;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

public sealed class AutomationTaskAggregateId : Identity<AutomationTaskAggregateId>
{
    private static readonly Guid Namespace = Guid.Parse("ba0ec8c7-9c67-48b7-a9f2-64618eb4f5a5");

    public AutomationTaskAggregateId(string value)
        : base(value)
    {
    }

    public static AutomationTaskAggregateId With(string tenantId, string taskId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new ArgumentException("Task id cannot be empty.", nameof(taskId));
        }

        var composite = $"{tenantId}:{taskId}";
        return NewDeterministic(Namespace, composite);
    }
}
