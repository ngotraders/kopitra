namespace Kopitra.ManagementApi.Accounts;

public sealed record ManagedAccountSummary(
    string AccountId,
    string DisplayName,
    string Broker,
    string Platform,
    AccountStatus Status,
    IReadOnlyList<string> Tags,
    AccountMetrics Metrics,
    DateTimeOffset UpdatedAt);
