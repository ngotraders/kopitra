using Kopitra.ManagementApi.Domain.CopyTrading;

namespace Kopitra.ManagementApi.Infrastructure.ReadModels;

public sealed record CopyTradeGroupMemberReadModel(
    string MemberId,
    CopyTradeMemberRole Role,
    RiskStrategy RiskStrategy,
    decimal Allocation,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
