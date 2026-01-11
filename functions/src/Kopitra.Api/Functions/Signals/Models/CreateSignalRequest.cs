namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Request for creating a new signal
/// </summary>
public class CreateSignalRequest
{
    /// <summary>
    /// The ID of the provider creating the signal
    /// </summary>
    public string ProviderId { get; set; } = null!;

    /// <summary>
    /// Trading symbol (e.g., "EURUSD")
    /// </summary>
    public string Symbol { get; set; } = null!;

    /// <summary>
    /// Order action (Open, Close, Modify)
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Position size in lots
    /// </summary>
    public decimal PositionSize { get; set; }

    /// <summary>
    /// Risk management details (JSON string or object)
    /// </summary>
    public string RiskManagement { get; set; } = null!;
}
