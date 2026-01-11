namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Position size configuration for signal
/// </summary>
public class SignalPositionSizeRequest
{
    public decimal? FixedLot { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsProportional { get; set; }
}
