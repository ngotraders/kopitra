using Kopitra.Api.Domain;
using Kopitra.Api.Infrastructure;
using EventFlow.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Tests;

public class TestDbContextProvider : IDbContextProvider<KopitraDbContext>
{
    private readonly DbContextOptions<KopitraDbContext> _options;

    public TestDbContextProvider(DbContextOptions<KopitraDbContext> options)
    {
        _options = options;
    }

    public KopitraDbContext CreateContext()
    {
        return new KopitraDbContext(_options);
    }
}

public static class TestServiceProvider
{
    public static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Use in-memory database for testing
        var dbOptions = new DbContextOptionsBuilder<KopitraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        // Register DbContextOptions so TestDbContextProvider can receive it
        services.AddSingleton(dbOptions);
        
        // Register the provider instance explicitly, like strattrack does
        services.AddSingleton<TestDbContextProvider>();
        
        // Let AddKopitra register the provider
        services.AddKopitra<TestDbContextProvider>();
        services.AddSingleton<Functions.ExpertAdvisorFunctions>();
        var serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        var context = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>().CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.Dispose();
        
        return serviceProvider;
    }
}
