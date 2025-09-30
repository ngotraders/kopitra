using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.ExpertAdvisors;
using Kopitra.ManagementApi.Infrastructure.Messaging;
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
        using var provider = ManagementApiTestServiceProvider.Build();
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = provider.GetRequiredService<TestClock>();
        clock.SetTime(new DateTimeOffset(2024, 01, 01, 0, 0, 0, TimeSpan.Zero));

        var registerResult = await commandDispatcher.DispatchAsync(
            new RegisterExpertAdvisorCommand("tenant-1", "ea-1", "My EA", "Test", "alice"),
            CancellationToken.None);
        Assert.Equal(ExpertAdvisorStatus.PendingApproval, registerResult.Status);
        Assert.Equal("My EA", registerResult.DisplayName);

        var pending = await queryDispatcher.DispatchAsync(
            new GetExpertAdvisorQuery("tenant-1", "ea-1"),
            CancellationToken.None);
        Assert.NotNull(pending);
        Assert.Equal(ExpertAdvisorStatus.PendingApproval, pending!.Status);
        Assert.Equal("Test", pending.Description);

        clock.Advance(TimeSpan.FromDays(1));
        var approveResult = await commandDispatcher.DispatchAsync(
            new ApproveExpertAdvisorCommand("tenant-1", "ea-1", "bob"),
            CancellationToken.None);
        Assert.Equal(ExpertAdvisorStatus.Approved, approveResult.Status);
        Assert.Equal("bob", approveResult.ApprovedBy);

        var approved = await queryDispatcher.DispatchAsync(
            new GetExpertAdvisorQuery("tenant-1", "ea-1"),
            CancellationToken.None);
        Assert.NotNull(approved);
        Assert.Equal(ExpertAdvisorStatus.Approved, approved!.Status);
        Assert.Equal("bob", approved.ApprovedBy);

        var bus = (InMemoryServiceBusPublisher)provider.GetRequiredService<IServiceBusPublisher>();
        var messages = bus.DequeueAll();
        Assert.Equal(4, messages.Count);
        Assert.All(messages, m => Assert.Equal("expert-advisors", m.Topic));
        Assert.Equal(
            new[]
            {
                "expert-advisor-registered",
                "expert-advisor-status-changed",
                "expert-advisor-approved",
                "expert-advisor-status-changed"
            },
            messages.Select(m => m.Payload.GetType().GetProperty("Type")?.GetValue(m.Payload)?.ToString()).ToArray());
    }

    [Fact]
    public async Task UpdateExpertAdvisorStatus_PublishesStatusChangeAndUpdatesReadModel()
    {
        using var provider = ManagementApiTestServiceProvider.Build();
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = provider.GetRequiredService<TestClock>();
        clock.SetTime(new DateTimeOffset(2024, 02, 01, 0, 0, 0, TimeSpan.Zero));

        await commandDispatcher.DispatchAsync(
            new RegisterExpertAdvisorCommand("tenant-1", "ea-42", "EA-42", "Growth", "carol"),
            CancellationToken.None);

        var beforeUpdate = await queryDispatcher.DispatchAsync(
            new GetExpertAdvisorQuery("tenant-1", "ea-42"),
            CancellationToken.None);
        Assert.NotNull(beforeUpdate);
        var previousUpdatedAt = beforeUpdate!.UpdatedAt;

        var bus = (InMemoryServiceBusPublisher)provider.GetRequiredService<IServiceBusPublisher>();
        bus.DequeueAll();

        clock.Advance(TimeSpan.FromHours(3));
        var updated = await commandDispatcher.DispatchAsync(
            new UpdateExpertAdvisorStatusCommand("tenant-1", "ea-42", ExpertAdvisorStatus.Suspended, "risk breach", "dave"),
            CancellationToken.None);
        Assert.Equal(ExpertAdvisorStatus.Suspended, updated.Status);

        var readModel = await queryDispatcher.DispatchAsync(
            new GetExpertAdvisorQuery("tenant-1", "ea-42"),
            CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal(ExpertAdvisorStatus.Suspended, readModel!.Status);
        Assert.True(readModel.UpdatedAt > previousUpdatedAt);

        var statusMessages = bus.DequeueAll();
        Assert.Single(statusMessages);
        var payloadType = statusMessages.Single().Payload.GetType().GetProperty("Type")?.GetValue(statusMessages.Single().Payload)?.ToString();
        Assert.Equal("expert-advisor-status-changed", payloadType);
    }
}
