using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Application.Integration.Commands;
using Kopitra.ManagementApi.Application.Integration.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Application;

public class IntegrationCommandHandlerTests
{
    [Fact]
    public async Task RecordIntegrationEvents_PersistsAndOrdersByOccurredAtThenReceivedAt()
    {
        using var provider = ManagementApiTestServiceProvider.Build();
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = provider.GetRequiredService<TestClock>();
        clock.SetTime(new DateTimeOffset(2024, 05, 01, 0, 0, 0, TimeSpan.Zero));

        var first = await commandDispatcher.DispatchAsync(
            new RecordEaIntegrationEventCommand(
                "tenant-1",
                "ea",
                "status-updated",
                "{}",
                new DateTimeOffset(2024, 05, 01, 6, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        Assert.Equal(clock.UtcNow, first.ReceivedAt);

        clock.Advance(TimeSpan.FromMinutes(10));
        var second = await commandDispatcher.DispatchAsync(
            new RecordEaIntegrationEventCommand(
                "tenant-1",
                "ea",
                "heartbeat",
                "{}",
                new DateTimeOffset(2024, 05, 01, 6, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        Assert.Equal(clock.UtcNow, second.ReceivedAt);

        clock.Advance(TimeSpan.FromMinutes(10));
        var third = await commandDispatcher.DispatchAsync(
            new RecordEaIntegrationEventCommand(
                "tenant-1",
                "ea",
                "drawdown",
                "{}",
                new DateTimeOffset(2024, 04, 30, 23, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        Assert.Equal(clock.UtcNow, third.ReceivedAt);

        var events = await queryDispatcher.DispatchAsync(
            new ListEaIntegrationEventsQuery("tenant-1"),
            CancellationToken.None);
        Assert.Equal(3, events.Count);
        Assert.Equal(
            new[] { second.EventType, first.EventType, third.EventType },
            events.Select(e => e.EventType).ToArray());
        Assert.True(events.First().ReceivedAt > events.Skip(1).First().ReceivedAt);
        Assert.True(events.Skip(1).First().OccurredAt > events.Last().OccurredAt);
    }
}
