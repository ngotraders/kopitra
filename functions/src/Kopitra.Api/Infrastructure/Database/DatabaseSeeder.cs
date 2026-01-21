using EventFlow.EntityFramework;
using Kopitra.Api.Application;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Application.Users.Services;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kopitra.Api.Infrastructure.Database;

/// <summary>
/// Seeds initial data into the database
/// </summary>
public class DatabaseSeeder
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IDbContextProvider<KopitraDbContext> contextProvider,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _contextProvider = contextProvider;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seeds initial admin user if it doesn't exist
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            using var context = _contextProvider.CreateContext();

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();

            // Check if any users exist
            var hasUsers = await context.Users.AnyAsync();
            if (hasUsers)
            {
                _logger.LogInformation("Database already has users. Skipping seed.");
                return;
            }

            // Get admin credentials from configuration
            var adminEmail = _configuration["InitialAdmin:Email"] ?? "admin@kopitra.local";
            var adminPassword = _configuration["InitialAdmin:Password"] ?? "P@ssw0rd";
            var adminDisplayName = _configuration["InitialAdmin:DisplayName"] ?? "System Administrator";

            // Create initial admin user
            var adminUserId = UserId.New.Value;
            var hashedPassword = _passwordHasher.Hash(adminPassword);

            var adminUser = new UserReadModel
            {
                Id = adminUserId,
                Email = adminEmail,
                DisplayName = adminDisplayName,
                PasswordHash = hashedPassword,
                CanProvide = true,
                CanSubscribe = true,
                IsActive = true,
                Roles = new[] { "Admin" },
                RegisteredAt = DateTimeOffset.UtcNow,
                LastLoginAt = null
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Initial admin user created successfully. Email: {Email}",
                adminEmail);

            // Log password only in development
            if (_configuration["AZURE_FUNCTIONS_ENVIRONMENT"] == "Development")
            {
                _logger.LogWarning(
                    "Initial admin password: {Password} (Change this immediately!)",
                    adminPassword);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database");
            throw;
        }
    }
}
