using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class RecordUserLoginCommandTests
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
    public async Task RecordUserLogin_WithActiveUser_RecordsSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "login@example.com",
            Name = "Login Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new RecordUserLoginCommand(userId);

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsNotNull(user.LastLoginAt);
    }

    [TestMethod]
    public async Task RecordUserLogin_UpdatesTimestamp()
    {
        // Arrange - Create user
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "timestamp@example.com",
            Name = "Timestamp Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var beforeLogin = DateTimeOffset.UtcNow;
        var command = new RecordUserLoginCommand(userId);

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);
        var afterLogin = DateTimeOffset.UtcNow;

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsNotNull(user.LastLoginAt);
        Assert.IsTrue(user.LastLoginAt >= beforeLogin);
        Assert.IsTrue(user.LastLoginAt <= afterLogin);
    }

    [TestMethod]
    public async Task RecordUserLogin_MultipleLogins_UpdatesTimestamp()
    {
        // Arrange - Create user and record first login
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "multilogin@example.com",
            Name = "Multi Login",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var firstLoginCommand = new RecordUserLoginCommand(userId);
        await _commandBus.PublishAsync(firstLoginCommand, CancellationToken.None);

        var query1 = new GetUserByIdQuery(userId);
        var userAfterFirstLogin = await _queryProcessor.ProcessAsync(query1, CancellationToken.None);
        var firstLoginTime = userAfterFirstLogin.LastLoginAt;

        // Wait a moment to ensure timestamp difference
        await Task.Delay(100);

        var secondLoginCommand = new RecordUserLoginCommand(userId);

        // Act
        await _commandBus.PublishAsync(secondLoginCommand, CancellationToken.None);

        // Assert
        var query2 = new GetUserByIdQuery(userId);
        var userAfterSecondLogin = await _queryProcessor.ProcessAsync(query2, CancellationToken.None);

        Assert.IsNotNull(userAfterSecondLogin);
        Assert.IsNotNull(userAfterSecondLogin.LastLoginAt);
        Assert.IsTrue(userAfterSecondLogin.LastLoginAt > firstLoginTime);
    }

    [TestMethod]
    public async Task RecordUserLogin_WhenInactive_ThrowsException()
    {
        // Arrange - Create and deactivate user
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "inactivelogin@example.com",
            Name = "Inactive Login",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var deactivateCommand = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Test deactivation"
        };
        await _commandBus.PublishAsync(deactivateCommand, CancellationToken.None);

        var command = new RecordUserLoginCommand(userId);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }
}
