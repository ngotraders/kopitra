using Kopitra.ManagementApi.Infrastructure.Authentication;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Infrastructure;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ReturnsDistinctHashesForDifferentSalts()
    {
        var hash1 = PasswordHasher.HashPassword("s3cure-P@ss");
        var hash2 = PasswordHasher.HashPassword("s3cure-P@ss");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("MySecret123!");
        Assert.True(PasswordHasher.VerifyPassword("MySecret123!", hash));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        var hash = PasswordHasher.HashPassword("CorrectHorseBatteryStaple");
        Assert.False(PasswordHasher.VerifyPassword("Tr0ub4dor&3", hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-format")]
    [InlineData("PBKDF2-SHA256:not-a-number:salt:hash")]
    public void VerifyPassword_ReturnsFalseForInvalidHash(string? invalidHash)
    {
        Assert.False(PasswordHasher.VerifyPassword("password", invalidHash ?? string.Empty));
    }
}
