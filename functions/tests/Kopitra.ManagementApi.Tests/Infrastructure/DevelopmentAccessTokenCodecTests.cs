using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Infrastructure.Authentication;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Infrastructure;

public class DevelopmentAccessTokenCodecTests
{
    [Fact]
    public void CreateTokenAndDecode_RoundTripsDescriptor()
    {
        var issuedAt = new DateTimeOffset(2024, 04, 22, 9, 30, 0, TimeSpan.Zero);
        var descriptor = new DevelopmentAccessTokenDescriptor(
            "tenant-1",
            "user-1",
            "Alex Morgan",
            "alex@example.com",
            new[] { "operator", "admin" },
            issuedAt);

        var token = DevelopmentAccessTokenCodec.CreateToken(descriptor);
        Assert.StartsWith("kopitra-dev.", token, StringComparison.Ordinal);

        var decoded = DevelopmentAccessTokenCodec.DecodeToken(token);
        Assert.Equal(descriptor.TenantId, decoded.TenantId);
        Assert.Equal(descriptor.UserId, decoded.UserId);
        Assert.Equal(descriptor.DisplayName, decoded.DisplayName);
        Assert.Equal(descriptor.Email, decoded.Email);
        Assert.Equal(descriptor.Roles, decoded.Roles);
        Assert.Equal(descriptor.IssuedAt, decoded.IssuedAt);
    }

    [Fact]
    public async Task ValidateAsync_PopulatesClaimsFromToken()
    {
        var descriptor = new DevelopmentAccessTokenDescriptor(
            "tenant-ops",
            "user-42",
            "Jordan Mills",
            "jordan@example.com",
            new[] { "operator", "analyst" },
            DateTimeOffset.UtcNow);

        var token = DevelopmentAccessTokenCodec.CreateToken(descriptor);
        var validator = new DevelopmentAccessTokenValidator();

        var principal = await validator.ValidateAsync(token, CancellationToken.None);
        Assert.Equal("user-42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Jordan Mills", principal.Identity?.Name);
        Assert.Equal("jordan@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
        var roles = principal.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        Assert.Contains("operator", roles);
        Assert.Contains("analyst", roles);
    }
}
