using EventFlow.EntityFramework;
using EventFlow.EntityFramework.Extensions;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Domain.ExpertAdvisors.Commands;
using Kopitra.Api.Domain.ExpertAdvisors.Events;
using Kopitra.Api.Domain.ExpertAdvisors.Queries;
using Kopitra.Api.Common;

namespace Kopitra.Api.Domain;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddKopitra<TDbContextProvider>(this IServiceCollection services)
        where TDbContextProvider : class, IDbContextProvider<KopitraDbContext>
    {
        services.AddScoped<IClock, Clock>();
        return services.AddEventFlow(ef =>
        {
            ef.AddCommands(
                typeof(CreateSessionCommand),
                typeof(AuthenticateSessionCommand),
                typeof(RecordHeartbeatCommand),
                typeof(CloseSessionCommand)
            ).AddCommandHandlers(
                typeof(CreateSessionCommandHandler),
                typeof(AuthenticateSessionCommandHandler),
                typeof(RecordHeartbeatCommandHandler),
                typeof(CloseSessionCommandHandler)
            ).AddEvents(
                typeof(SessionCreatedEvent),
                typeof(SessionAuthenticatedEvent),
                typeof(HeartbeatReceivedEvent),
                typeof(SessionClosedEvent)
            );

            ef.ConfigureEntityFramework(EntityFrameworkConfiguration.New);
            ef.UseEntityFrameworkEventStore<KopitraDbContext>();
            ef.AddDbContextProvider<KopitraDbContext, TDbContextProvider>();

            ef.UseEntityFrameworkReadModel<ExpertAdvisorSessionReadModel, KopitraDbContext>();
            ef.AddQueryHandlers(
                typeof(GetSessionByIdQueryHandler),
                typeof(GetAllSessionsQueryHandler)
            );
        });
    }
}
