using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kopitra.ManagementApi.Infrastructure.Eventing;

public sealed class EventDefinitionSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type[] _eventTypes;

    public EventDefinitionSeeder(IServiceProvider serviceProvider, Type[] eventTypes)
    {
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes.ToArray();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var definitionService = scope.ServiceProvider.GetRequiredService<IEventDefinitionService>();
        definitionService.Load(_eventTypes);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
