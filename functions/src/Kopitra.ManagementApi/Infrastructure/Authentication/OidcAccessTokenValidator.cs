using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public sealed class OidcAccessTokenValidator : IAccessTokenValidator
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly ILogger<OidcAccessTokenValidator> _logger;
    private readonly ManagementAuthenticationOptions _options;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public OidcAccessTokenValidator(IOptions<ManagementAuthenticationOptions> options, ILogger<OidcAccessTokenValidator> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var metadataAddress = ResolveMetadataAddress(_options);
        if (string.IsNullOrWhiteSpace(metadataAddress))
        {
            throw new InvalidOperationException("Authentication metadata address could not be determined. Configure an authority or metadata address.");
        }

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever());
    }

    public async Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(token));
        }

        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);

        var parameters = BuildTokenValidationParameters(configuration);

        try
        {
            var principal = _tokenHandler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Access token validation failed.");
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            _logger.LogWarning(ex, "Access token format is invalid.");
            throw new SecurityTokenException("The provided token is invalid.", ex);
        }
    }

    private TokenValidationParameters BuildTokenValidationParameters(OpenIdConnectConfiguration configuration)
    {
        var audiences = _options.Audiences ?? Array.Empty<string>();
        var validIssuers = _options.ValidIssuers ?? Array.Empty<string>();

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = _options.ClockSkew ?? TimeSpan.FromMinutes(2),
            ValidateAudience = audiences.Length > 0,
            ValidAudiences = audiences.Length > 0 ? audiences : null,
            ValidateIssuer = validIssuers.Length > 0 || !string.IsNullOrWhiteSpace(configuration.Issuer),
            ValidIssuers = validIssuers.Length > 0 ? validIssuers : (!string.IsNullOrWhiteSpace(configuration.Issuer) ? new[] { configuration.Issuer } : null)
        };
    }

    private static string? ResolveMetadataAddress(ManagementAuthenticationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
        {
            return options.MetadataAddress;
        }

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            return null;
        }

        var authority = options.Authority.TrimEnd('/');
        return $"{authority}/.well-known/openid-configuration";
    }
}
