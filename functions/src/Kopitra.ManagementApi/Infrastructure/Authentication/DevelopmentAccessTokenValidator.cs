using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public sealed class DevelopmentAccessTokenValidator : IAccessTokenValidator
{
    public Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token must be provided.", nameof(token));
        }

        var identity = new ClaimsIdentity("Development");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "dev-user"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Development Operator"));
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(principal);
    }
}
