namespace Kopitra.Api.Functions.ExpertAdvisors.Models;

/// <summary>
/// Request model for creating a new EA session.
/// </summary>
public class CreateSessionRequest
{
    /// <summary>
    /// User ID associated with this EA session.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Account ID linked to this EA session.
    /// Represents the MT4/MT5 trading account for this EA.
    /// </summary>
    public string? AccountId { get; set; }
}
