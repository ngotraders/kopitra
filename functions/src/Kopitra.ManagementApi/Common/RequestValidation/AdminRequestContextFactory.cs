using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Infrastructure.Authentication;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.IdentityModel.Tokens;

namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed class AdminRequestContextFactory
{
    private const string TenantHeader = "X-TradeAgent-Account";
    private const string AuthorizationHeader = "Authorization";
    private const string DefaultTenantId = "console";
    private readonly IAccessTokenValidator _tokenValidator;

    public AdminRequestContextFactory(IAccessTokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    public Task<AdminRequestContext> CreateAsync(HttpRequestData request, CancellationToken cancellationToken = default)
    {
        return CreateAsync(request.Headers, cancellationToken);
    }

    public async Task<AdminRequestContext> CreateAsync(HttpHeadersCollection headers, CancellationToken cancellationToken = default)
    {
        var tenant = ExtractHeaderValue(headers, TenantHeader);
        if (string.IsNullOrWhiteSpace(tenant))
        {
            tenant = DefaultTenantId;
        }

        var authorization = ExtractHeaderValue(headers, AuthorizationHeader);
        if (string.IsNullOrWhiteSpace(authorization))
        {
            throw new HttpRequestValidationException("missing_authorization", "Authorization header is required.", HttpStatusCode.Unauthorized);
        }

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestValidationException("invalid_authorization_scheme", "Authorization header must use the Bearer scheme.", HttpStatusCode.Unauthorized);
        }

        var token = authorization.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new HttpRequestValidationException("invalid_authorization_token", "Bearer token is missing.", HttpStatusCode.Unauthorized);
        }

        try
        {
            var principal = await _tokenValidator.ValidateAsync(token, cancellationToken).ConfigureAwait(false);
            return new AdminRequestContext(tenant, principal);
        }
        catch (SecurityTokenException ex)
        {
            throw new HttpRequestValidationException("invalid_token", ex.Message, HttpStatusCode.Unauthorized);
        }
        catch (InvalidOperationException ex)
        {
            throw new HttpRequestValidationException("authentication_configuration_error", ex.Message, HttpStatusCode.InternalServerError);
        }
    }

    private static string? ExtractHeaderValue(HttpHeadersCollection headers, string headerName)
    {
        if (!headers.TryGetValues(headerName, out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault();
        return value?.Trim();
    }
}
