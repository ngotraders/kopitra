using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Application;

public class CopyTradeGroupCommandHandlerTests
{
    [Fact]
    public async Task CreateAndManageMembers_UpdatesReadModelAndPublishesMemberMessages()
    {
        using var provider = ManagementApiTestServiceProvider.Build();
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = provider.GetRequiredService<TestClock>();
        clock.SetTime(new DateTimeOffset(2024, 03, 01, 0, 0, 0, TimeSpan.Zero));

        var created = await commandDispatcher.DispatchAsync(
            new CreateCopyTradeGroupCommand("tenant-1", "group-1", "Alpha", "Growth focus", "owner"),
            CancellationToken.None);
        Assert.Equal("Alpha", created.Name);
        Assert.Empty(created.Members);

        var readModel = await queryDispatcher.DispatchAsync(
            new GetCopyTradeGroupQuery("tenant-1", "group-1"),
            CancellationToken.None);
        Assert.NotNull(readModel);
        Assert.Equal("Alpha", readModel!.Name);
        Assert.Empty(readModel.Members);

        clock.Advance(TimeSpan.FromHours(2));
        var upserted = await commandDispatcher.DispatchAsync(
            new UpsertCopyTradeGroupMemberCommand(
                "tenant-1",
                "group-1",
                "ea-1",
                CopyTradeMemberRole.Leader,
                RiskStrategy.Conservative,
                0.35m,
                "owner"),
            CancellationToken.None);
        Assert.Single(upserted.Members);
        var member = upserted.Members.Single();
        Assert.Equal("ea-1", member.MemberId);
        Assert.Equal(CopyTradeMemberRole.Leader, member.Role);
        Assert.Equal(RiskStrategy.Conservative, member.RiskStrategy);
        Assert.Equal(0.35m, member.Allocation);

        clock.Advance(TimeSpan.FromHours(1));
        var removed = await commandDispatcher.DispatchAsync(
            new RemoveCopyTradeGroupMemberCommand("tenant-1", "group-1", "ea-1", "owner"),
            CancellationToken.None);
        Assert.Empty(removed.Members);

        var finalModel = await queryDispatcher.DispatchAsync(
            new GetCopyTradeGroupQuery("tenant-1", "group-1"),
            CancellationToken.None);
        Assert.NotNull(finalModel);
        Assert.Empty(finalModel!.Members);

        var bus = (InMemoryServiceBusPublisher)provider.GetRequiredService<IServiceBusPublisher>();
        var messages = bus.DequeueAll();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("copy-trade-members", m.Topic));
        Assert.Equal(
            new[] { "copy-trade-member-upserted", "copy-trade-member-removed" },
            messages.Select(m => m.Payload.GetType().GetProperty("Type")?.GetValue(m.Payload)?.ToString()).ToArray());
    }
}
