namespace Kopitra.Api.Functions.ExpertAdvisors.Models;

/// <summary>
/// Summary response for a session.
/// </summary>
public class SessionSummaryResponse
{
    /// <summary>
    /// The unique identifier for the session.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The user identifier associated with the session.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The account identifier associated with the session.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// The current state of the session (as integer).
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// The date and time when the session was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the session last received a heartbeat.
    /// </summary>
    public DateTimeOffset? LastHeartbeatAt { get; set; }

    /// <summary>
    /// The date and time when the session will expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
