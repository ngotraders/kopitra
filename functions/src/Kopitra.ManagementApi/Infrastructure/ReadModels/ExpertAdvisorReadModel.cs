using Kopitra.ManagementApi.Domain.ExpertAdvisors;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed record ExpertAdvisorReadModel(
    string TenantId,
    string ExpertAdvisorId,
    string DisplayName,
    string Description,
    ExpertAdvisorStatus Status,
    string? ApprovedBy,
    DateTimeOffset UpdatedAt);
