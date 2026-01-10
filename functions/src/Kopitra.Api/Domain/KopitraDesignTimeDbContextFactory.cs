using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kopitra.Api.Domain;

public class KopitraDesignTimeDbContextFactory : IDesignTimeDbContextFactory<KopitraDbContext>
{
    public KopitraDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KopitraDbConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=kopitra;Trusted_Connection=true;";
        
        var optionsBuilder = new DbContextOptionsBuilder<KopitraDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        return new KopitraDbContext(optionsBuilder.Options);
    }
}
