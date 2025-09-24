namespace Kopitra.ManagementApi.Accounts;

public sealed record AccountRiskSettings(
    decimal MaxDrawdownPercent,
    decimal MaxExposurePercent,
    decimal MaxPositionSizePercent);
