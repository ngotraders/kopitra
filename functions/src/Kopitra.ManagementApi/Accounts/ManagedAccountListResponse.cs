namespace Kopitra.ManagementApi.Accounts;

public sealed record ManagedAccountListResponse(int Count, IReadOnlyList<ManagedAccountSummary> Items);
