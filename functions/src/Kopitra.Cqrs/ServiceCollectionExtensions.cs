using Kopitra.Cqrs.Dispatching;
using Kopitra.Cqrs.EventStore;
using Kopitra.Cqrs.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Cqrs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCqrsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddScoped(typeof(AggregateRepository<,>));
        return services;
    }
}
