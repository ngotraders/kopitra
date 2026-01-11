using Kopitra.Api.Application.Users.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Kopitra.Api.Infrastructure.Security;

/// <summary>
/// JWT token generation service using symmetric HMAC signing.
/// Configuration from IConfiguration (JWT settings).
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _passphrase;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _issuer = _configuration["Jwt:Issuer"] ?? "kopitra-api";
        _audience = _configuration["Jwt:Audience"] ?? "kopitra-clients";
        _passphrase = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
        _expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");
    }

    /// <summary>
    /// Generate a JWT access token with given subject and optional claims.
    /// </summary>
    public string GenerateToken(string subject, Claim[]? claims = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));

        var passphraseBytes = Encoding.UTF8.GetBytes(_passphrase);
        var keyBytes = SHA256.HashData(passphraseBytes);
        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        if (claims != null)
            tokenClaims.AddRange(claims);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
