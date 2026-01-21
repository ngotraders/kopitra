using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class DeactivateUserCommandTests
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
    public async Task DeactivateUser_WithValidData_DeactivatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "deactivate@example.com",
            Name = "Deactivate Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Policy violation"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsFalse(user.IsActive);
    }

    [TestMethod]
    public async Task DeactivateUser_WhenAlreadyInactive_ThrowsException()
    {
        // Arrange - Create and deactivate user
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "doubleDe@example.com",
            Name = "Double Deactivate",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command1 = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "First deactivation"
        };
        await _commandBus.PublishAsync(command1, CancellationToken.None);

        var command2 = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Second deactivation"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _commandBus.PublishAsync(command2, CancellationToken.None);
        });
    }

    [TestMethod]
    public async Task DeactivateUser_RecordsDeactivationTimestamp()
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

        var command = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Test deactivation"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsFalse(user.IsActive);
        Assert.IsNotNull(user.LastAdminChangeAt);
    }
}
