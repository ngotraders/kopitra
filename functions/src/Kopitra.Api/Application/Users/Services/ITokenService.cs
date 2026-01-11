using System.Security.Claims;

namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Interface for JWT token generation service.
/// Implementation provided by Infrastructure layer.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT access token.
    /// </summary>
    /// <param name="subject">Subject (typically user ID or session ID).</param>
    /// <param name="claims">Optional additional claims to include in token.</param>
    /// <returns>JWT token string.</returns>
    string GenerateToken(string subject, Claim[]? claims = null);
}
