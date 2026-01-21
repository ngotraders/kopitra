using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class ReactivateUserCommandTests
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
    public async Task ReactivateUser_WhenInactive_ReactivatesSuccessfully()
    {
        // Arrange - Create and deactivate user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "reactivate@example.com",
            Name = "Reactivate Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var deactivateCommand = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Temporary suspension"
        };
        await _commandBus.PublishAsync(deactivateCommand, CancellationToken.None);

        var command = new ReactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Memo = "Suspension lifted"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public async Task ReactivateUser_WhenAlreadyActive_ThrowsException()
    {
        // Arrange - Create active user
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "alreadyactive@example.com",
            Name = "Already Active",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new ReactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Memo = "Trying to reactivate active user"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }

    [TestMethod]
    public async Task ReactivateUser_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange - Create user
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "workflow@example.com",
            Name = "Workflow Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        // Deactivate
        var deactivateCommand = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Test"
        };
        await _commandBus.PublishAsync(deactivateCommand, CancellationToken.None);

        // Reactivate
        var reactivateCommand = new ReactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Memo = "Restored"
        };
        await _commandBus.PublishAsync(reactivateCommand, CancellationToken.None);

        // Act - Deactivate again
        await _commandBus.PublishAsync(new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Second deactivation"
        }, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsFalse(user.IsActive);
    }
}
