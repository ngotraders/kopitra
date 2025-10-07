using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public sealed class DevelopmentAccessTokenValidator : IAccessTokenValidator
{
    public Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException("Token must be provided.");
        }

        var descriptor = DevelopmentAccessTokenCodec.DecodeToken(token);

        var identity = new ClaimsIdentity(authenticationType: "Development");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, descriptor.UserId));
        identity.AddClaim(new Claim(ClaimTypes.Name, descriptor.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Email, descriptor.Email));
        identity.AddClaim(new Claim("tenant", descriptor.TenantId));

        foreach (var role in descriptor.Roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
