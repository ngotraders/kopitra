namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Risk management DTO
/// </summary>
public class SignalRiskManagementDto
{
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
}
