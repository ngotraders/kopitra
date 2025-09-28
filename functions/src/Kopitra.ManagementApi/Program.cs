using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using Kopitra.ManagementApi.Accounts;
using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Automation.EventSourcing;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Diagnostics;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<IClock, UtcClock>();
        services.AddLogging();
        services.AddEventFlow(options => options
            .AddDefaults(typeof(AutomationTaskAggregate).Assembly)
            .RegisterServices(collection =>
            {
                collection.AddSingleton<IEventPersistence, InMemoryEventPersistence>();
            }));
        services.AddSingleton<IManagementRepository, InMemoryManagementRepository>();
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<IAutomationTaskService, AutomationTaskService>();
        services.AddSingleton<AdminRequestContextFactory>();
        services.AddSingleton<HealthReporter>();
        services.AddSingleton<IIdempotencyStore<AutomationTaskRunResponse>>(provider =>
            new InMemoryIdempotencyStore<AutomationTaskRunResponse>(TimeSpan.FromHours(24), provider.GetRequiredService<IClock>()));

        services.AddSingleton<IHealthContributor>(new StaticHealthContributor("api", true, "Worker process ready."));
        services.AddSingleton<IHealthContributor>(new StaticHealthContributor("database", true, "Azure SQL integration pending."));
        services.AddSingleton<IHealthContributor>(new StaticHealthContributor("service-bus", true, "Service Bus bindings deferred."));
        services.AddSingleton<IHealthContributor>(new StaticHealthContributor("storage", true, "Blob archival pipeline pending."));
    })
    .Build();

host.Run();
