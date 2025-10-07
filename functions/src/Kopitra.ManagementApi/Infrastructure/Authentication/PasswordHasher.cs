using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Kopitra.ManagementApi.Infrastructure.Authentication;

public static class PasswordHasher
{
    private const int SaltSize = 16; // 128-bit salt
    private const int KeySize = 32; // 256-bit subkey
    private const int DefaultIterations = 100_000;
    private const string AlgorithmMarker = "PBKDF2-SHA256";

    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var subkey = DeriveKey(password, salt, DefaultIterations);
        return string.Join(
            ':',
            AlgorithmMarker,
            DefaultIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(subkey));
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(':');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!string.Equals(parts[0], AlgorithmMarker, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedSubkey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = DeriveKey(password ?? string.Empty, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
}
