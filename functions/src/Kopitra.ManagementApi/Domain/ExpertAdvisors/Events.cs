using System;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace Kopitra.ManagementApi.Domain.ExpertAdvisors;

[EventVersion("expert-advisor-registered", 1)]
public sealed class ExpertAdvisorRegistered : AggregateEvent<ExpertAdvisorAggregate, ExpertAdvisorId>
{
    public ExpertAdvisorRegistered(string tenantId, string expertAdvisorId, string displayName, string description, string requestedBy, DateTimeOffset registeredAt)
    {
        TenantId = tenantId;
        ExpertAdvisorId = expertAdvisorId;
        DisplayName = displayName;
        Description = description;
        RequestedBy = requestedBy;
        RegisteredAt = registeredAt;
    }

    public string TenantId { get; }
    public string ExpertAdvisorId { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string RequestedBy { get; }
    public DateTimeOffset RegisteredAt { get; }
}

[EventVersion("expert-advisor-approved", 1)]
public sealed class ExpertAdvisorApproved : AggregateEvent<ExpertAdvisorAggregate, ExpertAdvisorId>
{
    public ExpertAdvisorApproved(string tenantId, string expertAdvisorId, string approvedBy, DateTimeOffset approvedAt)
    {
        TenantId = tenantId;
        ExpertAdvisorId = expertAdvisorId;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    public string TenantId { get; }
    public string ExpertAdvisorId { get; }
    public string ApprovedBy { get; }
    public DateTimeOffset ApprovedAt { get; }
}

[EventVersion("expert-advisor-status-changed", 1)]
public sealed class ExpertAdvisorStatusChanged : AggregateEvent<ExpertAdvisorAggregate, ExpertAdvisorId>
{
    public ExpertAdvisorStatusChanged(string tenantId, string expertAdvisorId, ExpertAdvisorStatus status, string? reason, DateTimeOffset changedAt)
    {
        TenantId = tenantId;
        ExpertAdvisorId = expertAdvisorId;
        Status = status;
        Reason = reason;
        ChangedAt = changedAt;
    }

    public string TenantId { get; }
    public string ExpertAdvisorId { get; }
    public ExpertAdvisorStatus Status { get; }
    public string? Reason { get; }
    public DateTimeOffset ChangedAt { get; }
}
