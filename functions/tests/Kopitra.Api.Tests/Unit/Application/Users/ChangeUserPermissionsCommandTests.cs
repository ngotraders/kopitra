using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class ChangeUserPermissionsCommandTests
{
    private ServiceProvider _serviceProvider = null!;
    private ICommandBus _commandBus = null!;
    private IQueryProcessor _queryProcessor = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceProvider = TestServiceProvider.CreateProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        _queryProcessor = _serviceProvider.GetRequiredService<IQueryProcessor>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task ChangeUserPermissions_EnableProvide_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "permissions@example.com",
            Name = "Permissions Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new ChangeUserPermissionsCommand(userId)
        {
            CanProvide = true,
            CanSubscribe = null
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.CanProvide);
    }

    [TestMethod]
    public async Task ChangeUserPermissions_DisableProvide_UpdatesSuccessfully()
    {
        // Arrange - Create user with provider enabled
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "disable@example.com",
            Name = "Disable Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        // Enable first
        var enableCommand = new ChangeUserPermissionsCommand(userId)
        {
            CanProvide = true,
            CanSubscribe = null
        };
        await _commandBus.PublishAsync(enableCommand, CancellationToken.None);

        // Disable
        var disableCommand = new ChangeUserPermissionsCommand(userId)
        {
            CanProvide = false,
            CanSubscribe = null
        };

        // Act
        await _commandBus.PublishAsync(disableCommand, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsFalse(user.CanProvide);
    }

    [TestMethod]
    public async Task ChangeUserPermissions_BothPermissions_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "both@example.com",
            Name = "Both Permissions",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new ChangeUserPermissionsCommand(userId)
        {
            CanProvide = true,
            CanSubscribe = true
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.CanProvide);
        Assert.IsTrue(user.CanSubscribe);
    }

    [TestMethod]
    public async Task ChangeUserPermissions_WithBothNull_ThrowsException()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "nullperms@example.com",
            Name = "Null Perms",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new ChangeUserPermissionsCommand(userId)
        {
            CanProvide = null,
            CanSubscribe = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }
}
