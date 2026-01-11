namespace Kopitra.Api.Functions.Signals.Models;

/// <summary>
/// Generic acknowledgment response for signal operations.
/// </summary>
public class SignalAckResponse
{
    /// <summary>
    /// Signal identifier associated with the operation.
    /// </summary>
    public string? SignalId { get; set; }

    /// <summary>
    /// Short message describing the outcome.
    /// </summary>
    public string? Message { get; set; }
}
