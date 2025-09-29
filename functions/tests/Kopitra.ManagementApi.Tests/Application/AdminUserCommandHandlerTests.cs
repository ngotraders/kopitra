using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Application.AdminUsers.Commands;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Application.Notifications.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Kopitra.ManagementApi.Time;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Application;

public class AdminUserCommandHandlerTests
{
    [Fact]
    public async Task ProvisionUpdateAndConfigureAdminUser_UpdatesReadModel()
    {
        using var provider = ManagementApiTestServiceProvider.Build();
        var commandDispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = provider.GetRequiredService<IQueryDispatcher>();
        var clock = provider.GetRequiredService<TestClock>();
        clock.SetTime(new DateTimeOffset(2024, 04, 01, 0, 0, 0, TimeSpan.Zero));

        var provisioned = await commandDispatcher.DispatchAsync(
            new ProvisionAdminUserCommand(
                "tenant-1",
                "user-1",
                "alice@example.com",
                "Alice",
                new[] { AdminUserRole.Operator, AdminUserRole.Auditor },
                "system"),
            CancellationToken.None);
        Assert.Equal("Alice", provisioned.DisplayName);
        Assert.False(provisioned.EmailEnabled);
        Assert.Equal(
            new[] { AdminUserRole.Operator, AdminUserRole.Auditor }.OrderBy(r => r).ToArray(),
            provisioned.Roles.OrderBy(r => r).ToArray());

        var list = await queryDispatcher.DispatchAsync(
            new ListAdminUsersQuery("tenant-1"),
            CancellationToken.None);
        var user = Assert.Single(list);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal(2, user.Roles.Count);

        clock.Advance(TimeSpan.FromHours(6));
        var updatedRoles = await commandDispatcher.DispatchAsync(
            new UpdateAdminUserRolesCommand(
                "tenant-1",
                "user-1",
                new[] { AdminUserRole.Supervisor },
                "system"),
            CancellationToken.None);
        Assert.Single(updatedRoles.Roles);
        Assert.Contains(AdminUserRole.Supervisor, updatedRoles.Roles);

        var afterRoleUpdate = await queryDispatcher.DispatchAsync(
            new ListAdminUsersQuery("tenant-1"),
            CancellationToken.None);
        var roleModel = Assert.Single(afterRoleUpdate);
        Assert.Single(roleModel.Roles);
        Assert.Contains(AdminUserRole.Supervisor, roleModel.Roles);
        var previousUpdatedAt = roleModel.UpdatedAt;

        clock.Advance(TimeSpan.FromHours(2));
        var configure = await commandDispatcher.DispatchAsync(
            new ConfigureAdminEmailNotificationsCommand(
                "tenant-1",
                "user-1",
                true,
                new[] { "Margin", "Drawdown" },
                "system"),
            CancellationToken.None);
        Assert.True(configure.EmailEnabled);
        Assert.Equal(
            new[] { "Drawdown", "Margin" },
            configure.Topics.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray());
        Assert.True(configure.UpdatedAt > previousUpdatedAt);

        var finalList = await queryDispatcher.DispatchAsync(
            new ListAdminUsersQuery("tenant-1"),
            CancellationToken.None);
        var finalModel = Assert.Single(finalList);
        Assert.True(finalModel.EmailEnabled);
        Assert.Equal(
            new[] { "Drawdown", "Margin" },
            finalModel.Topics.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray());
        Assert.True(finalModel.UpdatedAt >= configure.UpdatedAt);
    }
}
