namespace Kopitra.Api.Functions.Users.Models;

/// <summary>
/// Response model for user login.
/// Contains JWT tokens for API authentication.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for API requests.
    /// Expires in configured duration (default: 60 minutes).
    /// Include in Authorization header: `Bearer {AccessToken}`
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// Can be used to get a new AccessToken without re-entering credentials.
    /// Expires in configured duration (default: 30 days).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Expiration time of the access token in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }
}
