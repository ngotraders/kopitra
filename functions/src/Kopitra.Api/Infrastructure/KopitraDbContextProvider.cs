using EventFlow.EntityFramework;
using Kopitra.Api.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kopitra.Api.Infrastructure;

public class KopitraDbContextProvider : IDbContextProvider<KopitraDbContext>
{
    private readonly IConfiguration _configuration;

    public KopitraDbContextProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public KopitraDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<KopitraDbContext>();
        var connectionString = _configuration.GetConnectionString("KopitraDbConnection")
            ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=kopitra;Integrated Security=True;";
        optionsBuilder.UseSqlServer(connectionString);
        var context = new KopitraDbContext(optionsBuilder.Options);
        if (context.Database.IsSqlServer())
        {
            context.Database.Migrate();
        }
        else
        {
            context.Database.EnsureCreated();
        }
        return context;
    }
}
