using System.Collections.Generic;

namespace Kopitra.ManagementApi.Accounts;

public sealed record ManagedAccount(
    string AccountId,
    string DisplayName,
    string Broker,
    string Platform,
    AccountStatus Status,
    IReadOnlyList<string> Tags,
    AccountMetrics Metrics,
    AccountRiskSettings Risk,
    AccountSessionSnapshot Session,
    string? Description,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
