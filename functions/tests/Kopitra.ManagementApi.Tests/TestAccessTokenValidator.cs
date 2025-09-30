using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Infrastructure.Authentication;

namespace Kopitra.ManagementApi.Tests;

internal sealed class TestAccessTokenValidator : IAccessTokenValidator
{
    public Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "Test User")
        }, authenticationType: "Test");

        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
