namespace Kopitra.ManagementApi.Accounts;

public sealed record AccountMetrics(
    decimal AllocatedEquity,
    decimal DailyPnl,
    decimal WeeklyPnl,
    decimal MonthlyPnl,
    int OpenPositions,
    int ActiveFollowers,
    decimal DrawdownPercent);
