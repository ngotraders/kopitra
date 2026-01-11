namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Signal response DTO
/// </summary>
public class SignalDto
{
    public string SignalId { get; set; } = null!;
    public string ProviderId { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public int Action { get; set; }
    public SignalPositionSizeDto PositionSize { get; set; } = null!;
    public SignalRiskManagementDto? RiskManagement { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int DistributedToCount { get; set; }
    public int ExecutedCount { get; set; }
}
