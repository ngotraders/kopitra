namespace Kopitra.Api.Infrastructure.Persistence;

/// <summary>
/// Signal message from provider
/// </summary>
public class SignalMessage
{
    public string SignalId { get; set; } = null!;
    public string ProviderId { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public int Action { get; set; } // 0: Open, 1: Close, 2: Modify
    public decimal Lot { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
