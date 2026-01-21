using System.ComponentModel.DataAnnotations;

namespace Kopitra.Api.Functions.Users.Models;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// User email address (unique identifier).
    /// Must be a valid email format.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    /// <summary>
    /// User password (minimum 8 characters, hashed with PBKDF2-SHA256 on server).
    /// Never transmitted or stored in plain text.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string? Password { get; set; }

    /// <summary>
    /// User display name.
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    public string? Name { get; set; }
}
