namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Risk management configuration for signal
/// </summary>
public class SignalRiskManagementRequest
{
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
}
