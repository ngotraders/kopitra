namespace Kopitra.Api.Functions.Users.Models;

/// <summary>
/// Response model for user profile information.
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Unique user identifier (UUID).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether user has permission to distribute signals (provide/distribute).
    /// </summary>
    public bool CanProvide { get; set; }

    /// <summary>
    /// Whether user has permission to subscribe to signals.
    /// </summary>
    public bool CanSubscribe { get; set; }

    /// <summary>
    /// Whether the account is active (not deleted or disabled).
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets the date and time when the entity was registered.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// Gets the date and time of the user's last login, if available.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
