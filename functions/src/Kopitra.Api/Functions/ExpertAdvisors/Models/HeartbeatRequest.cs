namespace Kopitra.Api.Functions.ExpertAdvisors.Models;

/// <summary>
/// Request model for EA heartbeat (keepalive) signal.
/// Used to maintain session and confirm EA is active.
/// </summary>
public class HeartbeatRequest
{
    /// <summary>
    /// Expert Advisor session ID.
    /// Used to identify which EA session is sending the heartbeat.
    /// </summary>
    public string? SessionId { get; set; }
}
