using EventFlow.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Infrastructure;

public class KopitraDbContextProvider : IDbContextProvider<Domain.KopitraDbContext>
{
    private readonly IServiceProvider _serviceProvider;

    public KopitraDbContextProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Domain.KopitraDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<Domain.KopitraDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("KopitraDbConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=kopitra;Trusted_Connection=true;";
        optionsBuilder.UseSqlServer(connectionString);
        return new Domain.KopitraDbContext(optionsBuilder.Options);
    }
}
