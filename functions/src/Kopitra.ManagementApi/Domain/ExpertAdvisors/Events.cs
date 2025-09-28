using Kopitra.Cqrs.Events;

namespace Kopitra.ManagementApi.Domain.ExpertAdvisors;

public sealed record ExpertAdvisorRegistered(
    string TenantId,
    string ExpertAdvisorId,
    string DisplayName,
    string Description,
    string RequestedBy,
    DateTimeOffset RegisteredAt) : IDomainEvent;

public sealed record ExpertAdvisorApproved(
    string TenantId,
    string ExpertAdvisorId,
    string ApprovedBy,
    DateTimeOffset ApprovedAt) : IDomainEvent;

public sealed record ExpertAdvisorStatusChanged(
    string TenantId,
    string ExpertAdvisorId,
    ExpertAdvisorStatus Status,
    string? Reason,
    DateTimeOffset ChangedAt) : IDomainEvent;
