namespace Kopitra.ManagementApi.Accounts;

public sealed record AccountSessionSnapshot(
    int ActiveSessions,
    DateTimeOffset? LastHeartbeatAt,
    bool RequiresApproval,
    string? LastSignalId);
