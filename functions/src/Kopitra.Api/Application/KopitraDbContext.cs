using EventFlow.EntityFramework.Extensions;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Application.ExpertAdvisors.Queries;
using Kopitra.Api.Application.Users.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Application;

public class KopitraDbContext : DbContext
{
    public KopitraDbContext(DbContextOptions<KopitraDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccountReadModel> Accounts { get; set; }
    public DbSet<ActivationCodeReadModel> ActivationCodes { get; set; }
    public DbSet<UserReadModel> Users { get; set; }
    public DbSet<UserSessionReadModel> UserSessions { get; set; }
    public DbSet<ExpertAdvisorSessionReadModel> ExpertAdvisorSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEventFlowEvents();
        modelBuilder.AddEventFlowSnapshots();

        // Account configuration
        modelBuilder.Entity<AccountReadModel>()
            .Property(a => a.Balance)
            .HasPrecision(14, 2);
        modelBuilder.Entity<AccountReadModel>()
            .HasIndex(a => a.UserId);
        modelBuilder.Entity<AccountReadModel>()
            .HasIndex(a => new { a.AccountNumber, a.ServerName });

        // ActivationCode configuration
        modelBuilder.Entity<ActivationCodeReadModel>()
            .HasIndex(a => a.Code)
            .IsUnique();
        modelBuilder.Entity<ActivationCodeReadModel>()
            .HasIndex(a => a.UserId);
        modelBuilder.Entity<ActivationCodeReadModel>()
            .HasIndex(a => new { a.Status, a.ExpiresAt });

        // User configuration
        modelBuilder.Entity<UserReadModel>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<UserSessionReadModel>()
            .HasIndex(u => u.SessionId);
        modelBuilder.Entity<UserSessionReadModel>()
            .HasIndex(u => u.RefreshToken);
    }
}
