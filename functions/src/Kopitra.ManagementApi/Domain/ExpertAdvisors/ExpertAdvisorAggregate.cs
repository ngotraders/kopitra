using Kopitra.Cqrs.Aggregates;

namespace Kopitra.ManagementApi.Domain.ExpertAdvisors;

public sealed class ExpertAdvisorAggregate : AggregateRoot<string>
{
    public string TenantId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string RequestedBy { get; private set; } = string.Empty;
    public bool Approved { get; private set; }
    public string? ApprovedBy { get; private set; }
    public ExpertAdvisorStatus Status { get; private set; } = ExpertAdvisorStatus.Draft;

    public void Register(string tenantId, string expertAdvisorId, string displayName, string description, string requestedBy, DateTimeOffset registeredAt)
    {
        EnsureInitialized(expertAdvisorId);
        if (!string.IsNullOrEmpty(TenantId))
        {
            throw new InvalidOperationException($"Expert advisor {expertAdvisorId} already registered.");
        }

        Emit(new ExpertAdvisorRegistered(tenantId, expertAdvisorId, displayName, description, requestedBy, registeredAt));
        Emit(new ExpertAdvisorStatusChanged(tenantId, expertAdvisorId, ExpertAdvisorStatus.PendingApproval, "Awaiting approval", registeredAt));
    }

    public void Approve(string approvedBy, DateTimeOffset approvedAt)
    {
        if (Status == ExpertAdvisorStatus.Retired)
        {
            throw new InvalidOperationException("Cannot approve retired expert advisor.");
        }

        if (Approved)
        {
            return;
        }

        Emit(new ExpertAdvisorApproved(TenantId, Id, approvedBy, approvedAt));
        Emit(new ExpertAdvisorStatusChanged(TenantId, Id, ExpertAdvisorStatus.Approved, null, approvedAt));
    }

    public void ChangeStatus(ExpertAdvisorStatus status, string? reason, DateTimeOffset changedAt)
    {
        if (Status == status)
        {
            return;
        }

        Emit(new ExpertAdvisorStatusChanged(TenantId, Id, status, reason, changedAt));
    }

    private void Apply(ExpertAdvisorRegistered @event)
    {
        TenantId = @event.TenantId;
        DisplayName = @event.DisplayName;
        Description = @event.Description;
        RequestedBy = @event.RequestedBy;
        Status = ExpertAdvisorStatus.PendingApproval;
    }

    private void Apply(ExpertAdvisorApproved @event)
    {
        Approved = true;
        ApprovedBy = @event.ApprovedBy;
    }

    private void Apply(ExpertAdvisorStatusChanged @event)
    {
        Status = @event.Status;
    }
}
