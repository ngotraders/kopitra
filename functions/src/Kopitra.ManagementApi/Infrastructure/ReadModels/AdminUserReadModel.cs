using Kopitra.ManagementApi.Domain.AdminUsers;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed record AdminUserReadModel(
    string TenantId,
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyCollection<AdminUserRole> Roles,
    bool EmailEnabled,
    IReadOnlyCollection<string> Topics,
    DateTimeOffset UpdatedAt);
