namespace Kopitra.Api.Functions.Users.Models;

/// <summary>
/// Response model for user registration.
/// </summary>
public class RegisterUserResponse
{
    /// <summary>
    /// Registered user ID.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// User email address.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// User display name.
    /// </summary>
    public string Name { get; set; } = null!;
}
