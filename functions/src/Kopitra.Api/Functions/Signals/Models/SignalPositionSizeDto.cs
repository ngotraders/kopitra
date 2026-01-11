namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Position size DTO
/// </summary>
public class SignalPositionSizeDto
{
    public decimal? FixedLot { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsProportional { get; set; }
}
