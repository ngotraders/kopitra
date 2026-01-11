using System.Security.Cryptography;
using System.Text;
using Kopitra.Api.Application.Users.Services;

namespace Kopitra.Api.Infrastructure.Security;

/// <summary>
/// PBKDF2-based password hasher implementation.
/// Separates plaintext from hashed passwords using salt.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Hash a plaintext password using PBKDF2.
    /// </summary>
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);

        var saltAndHash = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, saltAndHash, 0, SaltSize);
        Array.Copy(hash, 0, saltAndHash, SaltSize, HashSize);

        return Convert.ToBase64String(saltAndHash);
    }

    /// <summary>
    /// Verify a plaintext password against a PBKDF2 hash.
    /// </summary>
    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            var saltAndHash = Convert.FromBase64String(hash);
            if (saltAndHash.Length != SaltSize + HashSize)
                return false;

            var salt = new byte[SaltSize];
            Array.Copy(saltAndHash, 0, salt, 0, SaltSize);

            var storedHash = new byte[HashSize];
            Array.Copy(saltAndHash, SaltSize, storedHash, 0, HashSize);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                Algorithm,
                HashSize);

            return computedHash.SequenceEqual(storedHash);
        }
        catch
        {
            return false;
        }
    }
}
