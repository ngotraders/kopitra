namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Signal list response
/// </summary>
public class SignalListResponse
{
    public List<SignalDto> Signals { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
