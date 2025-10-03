using System;
using System.Collections.Generic;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.Extensions;
using System.Net.Http;
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
using Kopitra.ManagementApi.Domain;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Infrastructure.Authentication;
using Kopitra.ManagementApi.Infrastructure.EventLog;
using Kopitra.ManagementApi.Infrastructure.Eventing;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Messaging.Http;
using Kopitra.ManagementApi.Infrastructure.Projections;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Infrastructure.Sessions;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kopitra.ManagementApi.DependencyInjection;

public static class ManagementApiServiceCollectionExtensions
{
    public static IServiceCollection AddManagementApiCore(
        this IServiceCollection services,
        bool includeHostedServices = true,
        Action<IServiceCollection>? configure = null)
    {
        services.AddLogging();

        services.TryAddSingleton<IClock, UtcClock>();
        services.TryAddSingleton<IExpertAdvisorReadModelStore, InMemoryExpertAdvisorReadModelStore>();
        services.TryAddSingleton<ICopyTradeGroupReadModelStore, InMemoryCopyTradeGroupReadModelStore>();
        services.TryAddSingleton<IAdminUserReadModelStore, InMemoryAdminUserReadModelStore>();
        services.TryAddSingleton<IEaIntegrationEventStore, InMemoryEaIntegrationEventStore>();
        services.TryAddSingleton<AdminRequestContextFactory>();
        services.TryAddSingleton<IExpertAdvisorSessionDirectory, InMemoryExpertAdvisorSessionDirectory>();

        services.TryAddSingleton<IConfiguration>(sp =>
        {
            var defaults = new Dictionary<string, string?>
            {
                ["ManagementApi:Authentication:Mode"] = "Development",
                ["ManagementApi:ServiceBus:EmulatorBaseUrl"] = string.Empty,
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(defaults)
                .Build();
        });

        services.AddOptions<ManagementAuthenticationOptions>()
            .BindConfiguration("ManagementApi:Authentication", binderOptions => binderOptions.ErrorOnUnknownConfiguration = false);

        services.AddOptions<ServiceBusOptions>()
            .BindConfiguration("ManagementApi:ServiceBus", binderOptions => binderOptions.ErrorOnUnknownConfiguration = false);

        services.AddHttpClient(nameof(HttpServiceBusPublisher));

        services.AddSingleton<IServiceBusPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.EmulatorBaseUrl))
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient(nameof(HttpServiceBusPublisher));
                var logger = sp.GetRequiredService<ILogger<HttpServiceBusPublisher>>();
                return new HttpServiceBusPublisher(client, sp.GetRequiredService<IOptions<ServiceBusOptions>>(), logger);
            }

            return new InMemoryServiceBusPublisher();
        });

        services.AddSingleton<CopyTradeGroupBroadcaster>();

        services.AddSingleton<IAccessTokenValidator>(sp =>
        {
            var authOptions = sp.GetRequiredService<IOptions<ManagementAuthenticationOptions>>().Value;
            if (string.Equals(authOptions.Mode, "Development", StringComparison.OrdinalIgnoreCase))
            {
                return new DevelopmentAccessTokenValidator();
            }

            return ActivatorUtilities.CreateInstance<OidcAccessTokenValidator>(sp);
        });

        services.TryAddScoped<ICommandDispatcher, CommandDispatcher>();
        services.TryAddScoped<IQueryDispatcher, QueryDispatcher>();

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

        var eventTypes = ManagementDomainEventTypes.All;
        services.AddEventFlow(options =>
            options.AddEvents(eventTypes)
                   .AddDefaults(typeof(ManagementApiServiceCollectionExtensions).Assembly));

        if (includeHostedServices)
        {
            services.AddSingleton<IHostedService>(sp => new EventDefinitionSeeder(sp, eventTypes));
        }

        configure?.Invoke(services);

        return services;
    }
}
