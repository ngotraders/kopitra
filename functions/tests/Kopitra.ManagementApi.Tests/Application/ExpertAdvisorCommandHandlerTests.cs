using System;
using System.Linq;
using System.Threading;
using Kopitra.Cqrs;
using Kopitra.Cqrs.Commands;
using Kopitra.Cqrs.Dispatching;
using Kopitra.Cqrs.EventStore;
using Kopitra.Cqrs.Events;
using Kopitra.Cqrs.Queries;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Projections;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
        services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<InMemoryServiceBusPublisher>();

        services.AddScoped(typeof(AggregateRepository<,>));

        services.AddScoped<ICommandHandler<RegisterExpertAdvisorCommand, ExpertAdvisorReadModel>, RegisterExpertAdvisorCommandHandler>();
        services.AddScoped<ICommandHandler<ApproveExpertAdvisorCommand, ExpertAdvisorReadModel>, ApproveExpertAdvisorCommandHandler>();
        services.AddScoped<IQueryHandler<GetExpertAdvisorQuery, ExpertAdvisorReadModel?>, GetExpertAdvisorQueryHandler>();

        services.AddScoped<IDomainEventHandler<ExpertAdvisorRegistered>, ExpertAdvisorProjection>();
        services.AddScoped<IDomainEventHandler<ExpertAdvisorApproved>, ExpertAdvisorProjection>();
        services.AddScoped<IDomainEventHandler<ExpertAdvisorStatusChanged>, ExpertAdvisorProjection>();
        services.AddScoped<IDomainEventHandler<ExpertAdvisorRegistered>, ExpertAdvisorMessagingHandler>();
        services.AddScoped<IDomainEventHandler<ExpertAdvisorApproved>, ExpertAdvisorMessagingHandler>();
        services.AddScoped<IDomainEventHandler<ExpertAdvisorStatusChanged>, ExpertAdvisorMessagingHandler>();

        var provider = services.BuildServiceProvider();
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
