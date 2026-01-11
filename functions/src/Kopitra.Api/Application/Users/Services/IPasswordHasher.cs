namespace Kopitra.Api.Application.Users.Services;

/// <summary>
/// Interface for password hashing service.
/// Implementation provided by Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plaintext password using PBKDF2 algorithm.
    /// </summary>
    /// <param name="password">Plaintext password to hash.</param>
    /// <returns>Hashed password (with salt embedded).</returns>
    string Hash(string password);

    /// <summary>
    /// Verify a plaintext password against a hash.
    /// </summary>
    /// <param name="password">Plaintext password to verify.</param>
    /// <param name="hash">Stored hash (from database).</param>
    /// <returns>True if password matches hash; false otherwise.</returns>
    bool Verify(string password, string hash);
}
