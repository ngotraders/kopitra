using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Azure.Functions.Worker.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Kopitra.Api.Common;

/// <summary>
/// Extension methods for HttpRequest to simplify JSON deserialization
/// </summary>
public static class HttpRequestDataExtensions
{
    /// <summary>
    /// Read and deserialize JSON from request body
    /// </summary>
    public static async Task<T?> ReadAsJsonAsync<T>(this HttpRequestData request, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
            return default;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Deserialize<T>(body, options);
    }

    /// <summary>
    /// Helper method to extract user ID from JWT token
    /// </summary>
    public static UserId? ExtractUserIdFromToken(this HttpRequestData req)
    {
        var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "authorization").Value?.First();
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        var jwtToken = authHeader.Replace("Bearer ", "");
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
        var sub = jsonToken?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(sub))
            return null;
        return UserId.With(sub);
    }
}
