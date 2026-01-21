using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class UpdateUserInfoCommandTests
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
    public async Task UpdateUserInfo_WithEmail_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "old@example.com",
            Name = "Old Name",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new UpdateUserInfoCommand(userId)
        {
            Email = "new@example.com",
            Name = null
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.AreEqual("new@example.com", user.Email);
        Assert.AreEqual("Old Name", user.Name);
    }

    [TestMethod]
    public async Task UpdateUserInfo_WithName_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "email@example.com",
            Name = "Old Name",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new UpdateUserInfoCommand(userId)
        {
            Email = null,
            Name = "New Name"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.AreEqual("email@example.com", user.Email);
        Assert.AreEqual("New Name", user.Name);
    }

    [TestMethod]
    public async Task UpdateUserInfo_WithBothFields_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "before@example.com",
            Name = "Before",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new UpdateUserInfoCommand(userId)
        {
            Email = "after@example.com",
            Name = "After"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.AreEqual("after@example.com", user.Email);
        Assert.AreEqual("After", user.Name);
    }

    [TestMethod]
    public async Task UpdateUserInfo_WithBothNull_ThrowsException()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "nullupdate@example.com",
            Name = "Null Update",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new UpdateUserInfoCommand(userId)
        {
            Email = null,
            Name = null
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }

    [TestMethod]
    public async Task UpdateUserInfo_CanBeQueriedByNewEmail()
    {
        // Arrange - Create user and update email
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "original@example.com",
            Name = "Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var updateCommand = new UpdateUserInfoCommand(userId)
        {
            Email = "updated@example.com",
            Name = null
        };
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Act
        var query = new GetUserByEmailQuery("updated@example.com");
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(userId.Value, user.Id);
        Assert.AreEqual("updated@example.com", user.Email);
    }
}
