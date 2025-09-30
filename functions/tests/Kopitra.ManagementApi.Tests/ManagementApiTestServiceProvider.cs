using System;
using EventFlow.EventStores;
using Kopitra.ManagementApi.Domain;
using Kopitra.ManagementApi.DependencyInjection;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.ManagementApi.Tests;

internal static class ManagementApiTestServiceProvider
{
    public static ServiceProvider Build(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddManagementApiCore(includeHostedServices: false, configure: svc =>
        {
            svc.AddSingleton<TestClock>();
            svc.AddSingleton<IClock>(sp => sp.GetRequiredService<TestClock>());
        });
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEventDefinitionService>().Load(ManagementDomainEventTypes.All);
        return provider;
    }
}
