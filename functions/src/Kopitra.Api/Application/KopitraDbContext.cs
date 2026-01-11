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

    public DbSet<ExpertAdvisorSessionReadModel> ExpertAdvisorSessions { get; set; }
    public DbSet<UserReadModel> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEventFlowEvents();
        modelBuilder.AddEventFlowSnapshots();
    }
}
