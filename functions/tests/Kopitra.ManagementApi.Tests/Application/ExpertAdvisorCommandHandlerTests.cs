using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.Extensions;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Projections;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EventFlow.EventStores;

namespace Kopitra.ManagementApi.Tests.Application;

public class ExpertAdvisorCommandHandlerTests
{
    [Fact]
    public async Task RegisterAndApproveExpertAdvisor_UpdatesReadModelAndPublishesMessages()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClock, TestClock>();
        services.AddSingleton<IServiceBusPublisher, InMemoryServiceBusPublisher>();
        services.AddSingleton<IExpertAdvisorReadModelStore, InMemoryExpertAdvisorReadModelStore>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped<ICommandHandler<RegisterExpertAdvisorCommand, ExpertAdvisorReadModel>, RegisterExpertAdvisorCommandHandler>();
        services.AddScoped<ICommandHandler<ApproveExpertAdvisorCommand, ExpertAdvisorReadModel>, ApproveExpertAdvisorCommandHandler>();
        services.AddScoped<IQueryHandler<GetExpertAdvisorQuery, ExpertAdvisorReadModel?>, GetExpertAdvisorQueryHandler>();

        services.AddEventFlow(options =>
            options.AddEvents(ManagementDomainEventTypes.All)
                   .AddDefaults(typeof(RegisterExpertAdvisorCommandHandler).Assembly));

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEventDefinitionService>().Load(ManagementDomainEventTypes.All);
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = (TestClock)provider.GetRequiredService<IClock>();
        clock.UtcNow = new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero);

        var registerResult = await commandDispatcher.DispatchAsync(new RegisterExpertAdvisorCommand("tenant-1", "ea-1", "My EA", "Test", "alice"), CancellationToken.None);
        Assert.Equal(ExpertAdvisorStatus.PendingApproval, registerResult.Status);

        var pending = await queryDispatcher.DispatchAsync(new GetExpertAdvisorQuery("tenant-1", "ea-1"), CancellationToken.None);
        Assert.NotNull(pending);
        Assert.Equal(ExpertAdvisorStatus.PendingApproval, pending!.Status);

        clock.UtcNow = clock.UtcNow.AddDays(1);
        var approveResult = await commandDispatcher.DispatchAsync(new ApproveExpertAdvisorCommand("tenant-1", "ea-1", "bob"), CancellationToken.None);
        Assert.Equal(ExpertAdvisorStatus.Approved, approveResult.Status);
        Assert.Equal("bob", approveResult.ApprovedBy);

        var approved = await queryDispatcher.DispatchAsync(new GetExpertAdvisorQuery("tenant-1", "ea-1"), CancellationToken.None);
        Assert.NotNull(approved);
        Assert.Equal(ExpertAdvisorStatus.Approved, approved!.Status);

        var bus = provider.GetRequiredService<IServiceBusPublisher>() as InMemoryServiceBusPublisher;
        Assert.NotNull(bus);
        var messages = bus!.DequeueAll();
        Assert.Equal(4, messages.Count);
        Assert.Single(messages.Select(m => m.Topic).Distinct(), "expert-advisors");
    }
}
