using System.ComponentModel.DataAnnotations;

namespace Kopitra.Api.Functions.Users.Models;

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    /// <summary>
    /// User password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string? Password { get; set; }
}
