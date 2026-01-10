using EventFlow.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Domain;

public class KopitraDbContext : DbContext
{
    public KopitraDbContext(DbContextOptions<KopitraDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExpertAdvisorSessionReadModel> ExpertAdvisorSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEventFlowEvents();
        modelBuilder.AddEventFlowSnapshots();
        modelBuilder.Entity<ExpertAdvisorSessionReadModel>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasMaxLength(255);
            entity.Property(m => m.UserId).HasMaxLength(255).IsRequired();
            entity.Property(m => m.AccountId).HasMaxLength(255).IsRequired();
            entity.Property(m => m.State).IsRequired();
            entity.Property(m => m.JwtToken).HasMaxLength(1000);
            entity.Property(m => m.CreatedAt).IsRequired();
            entity.Property(m => m.LastHeartbeatAt);
            entity.Property(m => m.ExpiresAt);
            entity.HasIndex(m => m.UserId);
            entity.HasIndex(m => m.AccountId);
            entity.HasIndex(m => m.State);
            entity.HasIndex(m => m.ExpiresAt);
        });
    }
}
