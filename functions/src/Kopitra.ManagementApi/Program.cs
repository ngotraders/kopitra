using System.Collections.Generic;
using EventFlow.DependencyInjection.Extensions;
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
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Domain;
using Kopitra.ManagementApi.Infrastructure.EventLog;
using Kopitra.ManagementApi.Infrastructure.Eventing;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Projections;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddLogging();
        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IServiceBusPublisher, InMemoryServiceBusPublisher>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IExpertAdvisorReadModelStore, InMemoryExpertAdvisorReadModelStore>();
        services.AddSingleton<ICopyTradeGroupReadModelStore, InMemoryCopyTradeGroupReadModelStore>();
        services.AddSingleton<IAdminUserReadModelStore, InMemoryAdminUserReadModelStore>();
        services.AddSingleton<IEaIntegrationEventStore, InMemoryEaIntegrationEventStore>();
        services.AddSingleton<AdminRequestContextFactory>();
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
        services.AddScoped<IQueryHandler<ListAdminUsersQuery, IReadOnlyCollection<AdminUserReadModel>>, ListAdminUsersQueryHandler>();
        services.AddScoped<ICommandHandler<ConfigureAdminEmailNotificationsCommand, AdminUserReadModel>, ConfigureAdminEmailNotificationsCommandHandler>();

        services.AddScoped<ICommandHandler<RecordEaIntegrationEventCommand, Kopitra.ManagementApi.Domain.Integration.EaIntegrationEvent>, RecordEaIntegrationEventCommandHandler>();
        services.AddScoped<IQueryHandler<ListEaIntegrationEventsQuery, IReadOnlyCollection<Kopitra.ManagementApi.Domain.Integration.EaIntegrationEvent>>, ListEaIntegrationEventsQueryHandler>();

        var eventTypes = ManagementDomainEventTypes.All;
        services.AddEventFlow(options =>
            options.AddEvents(eventTypes)
                   .AddDefaults(typeof(Program).Assembly));

        services.AddSingleton<IHostedService>(sp => new EventDefinitionSeeder(sp, eventTypes));
    })
    .Build();

host.Run();
