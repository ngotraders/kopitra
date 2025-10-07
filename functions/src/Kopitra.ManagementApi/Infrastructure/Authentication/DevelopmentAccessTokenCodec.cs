using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public sealed record DevelopmentAccessTokenDescriptor(
    string TenantId,
    string UserId,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> Roles,
    DateTimeOffset IssuedAt);

public static class DevelopmentAccessTokenCodec
{
    private const string Prefix = "kopitra-dev.";

    public static string CreateToken(DevelopmentAccessTokenDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Email))
        {
            throw new ArgumentException("Email is required.", nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
        {
            throw new ArgumentException("DisplayName is required.", nameof(descriptor));
        }

        var payload = new Payload
        {
            TenantId = descriptor.TenantId ?? string.Empty,
            UserId = descriptor.UserId,
            DisplayName = descriptor.DisplayName,
            Email = descriptor.Email,
            Roles = descriptor.Roles?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>(),
            IssuedAt = descriptor.IssuedAt,
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var encoded = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
        return Prefix + encoded;
    }

    public static DevelopmentAccessTokenDescriptor DecodeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException("Token is required.");
        }

        if (!token.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new SecurityTokenException("Token format is invalid.");
        }

        var encodedPayload = token.Substring(Prefix.Length);
        if (string.IsNullOrWhiteSpace(encodedPayload))
        {
            throw new SecurityTokenException("Token payload is missing.");
        }

        byte[] jsonBytes;
        try
        {
            jsonBytes = Base64UrlEncoder.DecodeBytes(encodedPayload);
        }
        catch (FormatException ex)
        {
            throw new SecurityTokenException("Token payload is malformed.", ex);
        }

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(jsonBytes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            throw new SecurityTokenException("Token payload could not be parsed.", ex);
        }

        if (payload is null)
        {
            throw new SecurityTokenException("Token payload is invalid.");
        }

        if (string.IsNullOrWhiteSpace(payload.UserId))
        {
            throw new SecurityTokenException("Token is missing a user identifier.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new SecurityTokenException("Token is missing an email address.");
        }

        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            throw new SecurityTokenException("Token is missing a display name.");
        }

        var roles = payload.Roles ?? Array.Empty<string>();

        return new DevelopmentAccessTokenDescriptor(
            payload.TenantId ?? string.Empty,
            payload.UserId,
            payload.DisplayName,
            payload.Email,
            roles,
            payload.IssuedAt == default ? DateTimeOffset.UtcNow : payload.IssuedAt);
    }

    private sealed class Payload
    {
        public string? TenantId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IReadOnlyCollection<string>? Roles { get; set; }

        public DateTimeOffset IssuedAt { get; set; }
    }
}
