namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Signal modification request
/// </summary>
public class SignalModifyRequest
{
    public SignalPositionSizeRequest? PositionSize { get; set; }
    public SignalRiskManagementRequest? RiskManagement { get; set; }
}
