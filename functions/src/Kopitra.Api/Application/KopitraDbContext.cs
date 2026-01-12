using EventFlow.EntityFramework.Extensions;
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

    public DbSet<UserReadModel> Users { get; set; }
    public DbSet<UserSessionReadModel> UserSessions { get; set; }
    public DbSet<ExpertAdvisorSessionReadModel> ExpertAdvisorSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEventFlowEvents();
        modelBuilder.AddEventFlowSnapshots();

        modelBuilder.Entity<UserReadModel>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<UserSessionReadModel>()
            .HasIndex(u => u.SessionId);
        modelBuilder.Entity<UserSessionReadModel>()
            .HasIndex(u => u.RefreshToken);
    }
}
