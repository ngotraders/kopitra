namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Signal creation request
/// </summary>
public class SignalCreateRequest
{
    public string Symbol { get; set; } = null!;
    public int Action { get; set; } // 0=Open, 1=Close, 2=Modify
    public SignalPositionSizeRequest PositionSize { get; set; } = null!;
    public SignalRiskManagementRequest? RiskManagement { get; set; }
}
