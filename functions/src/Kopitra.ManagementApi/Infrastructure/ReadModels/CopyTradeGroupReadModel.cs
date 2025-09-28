namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed record CopyTradeGroupReadModel(
    string TenantId,
    string GroupId,
    string Name,
    string? Description,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<CopyTradeGroupMemberReadModel> Members);
