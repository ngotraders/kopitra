using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Automation.EventSourcing;
using Kopitra.ManagementApi.Infrastructure;
using Kopitra.ManagementApi.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Automation;

public class AutomationTaskServiceTests
{
    private readonly TestClock _clock = new(new DateTimeOffset(2024, 1, 12, 7, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task RunTaskAsync_UpdatesRepositoryAndReturnsRunDetails()
    {
        var repository = new InMemoryManagementRepository(_clock);
        using var provider = BuildEventFlowProvider();
        var aggregateStore = provider.GetRequiredService<IAggregateStore>();
        var service = new AutomationTaskService(repository, _clock, aggregateStore);

        var response = await service.RunTaskAsync("demo", "daily-reconciliation", CancellationToken.None);

        Assert.Equal("daily-reconciliation", response.TaskId);
        Assert.Equal("Accepted", response.Status);
        Assert.Equal(_clock.UtcNow, response.SubmittedAt);
        Assert.False(string.IsNullOrWhiteSpace(response.RunId));

        var updated = await repository.FindAutomationTaskAsync("demo", "daily-reconciliation", CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Pending", updated!.LastExecution.Status);
        Assert.Equal(response.RunId, updated.LastExecution.RunId);

        var aggregateId = AutomationTaskAggregate.BuildId("demo", "daily-reconciliation");
        var aggregate = await aggregateStore.LoadAsync<AutomationTaskAggregate, AutomationTaskAggregateId>(aggregateId, CancellationToken.None);
        Assert.Equal("demo", aggregate.TenantId);
        Assert.Equal("daily-reconciliation", aggregate.TaskId);
        Assert.Equal(1, aggregate.Version);
        Assert.Single(aggregate.Executions);
        Assert.Equal(response.RunId, aggregate.Executions[0].RunId);
    }

    [Fact]
    public async Task RunTaskAsync_WhenTaskIsMissing_ThrowsNotFound()
    {
        var repository = new InMemoryManagementRepository(_clock);
        using var provider = BuildEventFlowProvider();
        var aggregateStore = provider.GetRequiredService<IAggregateStore>();
        var service = new AutomationTaskService(repository, _clock, aggregateStore);

        await Assert.ThrowsAsync<AutomationTaskNotFoundException>(() => service.RunTaskAsync("demo", "missing-task", CancellationToken.None));
    }

    private static ServiceProvider BuildEventFlowProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventFlow(options => options
            .AddDefaults(typeof(AutomationTaskAggregate).Assembly)
            .RegisterServices(collection =>
            {
                collection.AddSingleton<IEventPersistence, InMemoryEventPersistence>();
            }));

        return services.BuildServiceProvider();
    }
}
