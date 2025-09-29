using System;
using System.Collections.Generic;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.EventStores;
using EventFlow.Extensions;
using Kopitra.ManagementApi.Application.AdminUsers.Commands;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Application.Integration.Commands;
using Kopitra.ManagementApi.Application.Integration.Queries;
using Kopitra.ManagementApi.Application.Notifications.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain;
using Kopitra.ManagementApi.Infrastructure.EventLog;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Projections;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.ManagementApi.Tests;

internal static class ManagementApiTestServiceProvider
{
    public static ServiceProvider Build(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TestClock>();
        services.AddSingleton<IClock>(sp => sp.GetRequiredService<TestClock>());
        services.AddSingleton<IServiceBusPublisher, InMemoryServiceBusPublisher>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IExpertAdvisorReadModelStore, InMemoryExpertAdvisorReadModelStore>();
        services.AddSingleton<ICopyTradeGroupReadModelStore, InMemoryCopyTradeGroupReadModelStore>();
        services.AddSingleton<IAdminUserReadModelStore, InMemoryAdminUserReadModelStore>();
        services.AddSingleton<IEaIntegrationEventStore, InMemoryEaIntegrationEventStore>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped<ICommandHandler<RegisterExpertAdvisorCommand, ExpertAdvisorReadModel>, RegisterExpertAdvisorCommandHandler>();
        services.AddScoped<ICommandHandler<ApproveExpertAdvisorCommand, ExpertAdvisorReadModel>, ApproveExpertAdvisorCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateExpertAdvisorStatusCommand, ExpertAdvisorReadModel>, UpdateExpertAdvisorStatusCommandHandler>();
        services.AddScoped<IQueryHandler<GetExpertAdvisorQuery, ExpertAdvisorReadModel?>, GetExpertAdvisorQueryHandler>();
        services.AddScoped<IQueryHandler<ListExpertAdvisorsQuery, IReadOnlyCollection<ExpertAdvisorReadModel>>, ListExpertAdvisorsQueryHandler>();

        services.AddScoped<ICommandHandler<CreateCopyTradeGroupCommand, CopyTradeGroupReadModel>, CreateCopyTradeGroupCommandHandler>();
        services.AddScoped<ICommandHandler<UpsertCopyTradeGroupMemberCommand, CopyTradeGroupReadModel>, UpsertCopyTradeGroupMemberCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveCopyTradeGroupMemberCommand, CopyTradeGroupReadModel>, RemoveCopyTradeGroupMemberCommandHandler>();
        services.AddScoped<IQueryHandler<GetCopyTradeGroupQuery, CopyTradeGroupReadModel?>, GetCopyTradeGroupQueryHandler>();

        services.AddScoped<ICommandHandler<ProvisionAdminUserCommand, AdminUserReadModel>, ProvisionAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAdminUserRolesCommand, AdminUserReadModel>, UpdateAdminUserRolesCommandHandler>();
        services.AddScoped<ICommandHandler<ConfigureAdminEmailNotificationsCommand, AdminUserReadModel>, ConfigureAdminEmailNotificationsCommandHandler>();
        services.AddScoped<IQueryHandler<ListAdminUsersQuery, IReadOnlyCollection<AdminUserReadModel>>, ListAdminUsersQueryHandler>();

        services.AddScoped<ICommandHandler<RecordEaIntegrationEventCommand, Domain.Integration.EaIntegrationEvent>, RecordEaIntegrationEventCommandHandler>();
        services.AddScoped<IQueryHandler<ListEaIntegrationEventsQuery, IReadOnlyCollection<Domain.Integration.EaIntegrationEvent>>, ListEaIntegrationEventsQueryHandler>();

        services.AddEventFlow(options =>
            options.AddEvents(ManagementDomainEventTypes.All)
                   .AddDefaults(typeof(RegisterExpertAdvisorCommandHandler).Assembly));

        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEventDefinitionService>().Load(ManagementDomainEventTypes.All);
        return provider;
    }
}
