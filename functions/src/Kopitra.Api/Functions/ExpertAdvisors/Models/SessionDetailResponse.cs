namespace Kopitra.Api.Functions.ExpertAdvisors.Models;

/// <summary>
/// Response model with detailed EA session information.
/// </summary>
public class SessionDetailResponse
{
    /// <summary>
    /// EA session unique identifier (UUID).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// User ID associated with this session.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Account ID associated with this session.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Session state (integer code).
    /// 0 = Pending, 1 = Active, 2 = Closed, 3 = Error
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// JWT token for EA authentication (if authenticated).
    /// </summary>
    public string? JwtToken { get; set; }

    /// <summary>
    /// Session creation date and time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last heartbeat timestamp from this EA.
    /// </summary>
    public DateTimeOffset? LastHeartbeatAt { get; set; }

    /// <summary>
    /// Session expiration date and time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
