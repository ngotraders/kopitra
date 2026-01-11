using Kopitra.Api.Infrastructure;
using EventFlow.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Kopitra.Api.Application;

namespace Kopitra.Api.Tests;

public static class SqlServerTestServiceProvider
{
    /// <summary>
    /// SQL Server を使用してテスト用の ServiceProvider を作成します。
    /// 環境変数 KopitraDbConnection が設定されている場合、その値を使用します。
    /// 設定されていない場合は InMemoryDatabase にフォールバックします。
    /// </summary>
    public static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var connectionString = Environment.GetEnvironmentVariable("KopitraDbConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // InMemoryDatabase にフォールバック
            var dbOptions = new DbContextOptionsBuilder<KopitraDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            services.AddSingleton(dbOptions);
            services.AddSingleton<TestDbContextProvider>();
        }
        else
        {
            // SQL Server を使用
            var dbOptions = new DbContextOptionsBuilder<KopitraDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            services.AddSingleton(dbOptions);
            services.AddSingleton<SqlServerTestDbContextProvider>();
        }

        services.AddKopitra<TestDbContextProvider>();
        // services.AddSingleton<Functions.ExpertAdvisorFunctions>();
        var serviceProvider = services.BuildServiceProvider();

        // データベース初期化
        var contextProvider = serviceProvider.GetRequiredService<IDbContextProvider<KopitraDbContext>>();
        using (var context = contextProvider.CreateContext())
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                // SQL Server の場合は既存データをクリア
                context.Database.EnsureDeleted();
            }
            context.Database.EnsureCreated();
        }

        return serviceProvider;
    }
}

/// <summary>
/// SQL Server 用 DbContextProvider
/// </summary>
public class SqlServerTestDbContextProvider : IDbContextProvider<KopitraDbContext>
{
    private readonly DbContextOptions<KopitraDbContext> _options;

    public SqlServerTestDbContextProvider(DbContextOptions<KopitraDbContext> options)
    {
        _options = options;
    }

    public KopitraDbContext CreateContext()
    {
        return new KopitraDbContext(_options);
    }
}
