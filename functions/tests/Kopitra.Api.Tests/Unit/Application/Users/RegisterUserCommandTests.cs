using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class RegisterUserCommandTests
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
    public async Task RegisterUser_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var userId = UserId.New;
        var command = new RegisterUserCommand(userId)
        {
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hashed_password_123"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.AreEqual(userId.Value, user.Id);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.AreEqual("Test User", user.UserName);
        Assert.AreEqual("hashed_password_123", user.PasswordHash);
        Assert.IsTrue(user.IsActive);
        Assert.IsTrue(user.Roles.Contains("Subscriber"));
        Assert.IsTrue(user.CanSubscribe);
        Assert.IsFalse(user.CanProvide);
    }

    [TestMethod]
    public async Task RegisterUser_WithValidData_SetsRegistrationTimestamp()
    {
        // Arrange
        var userId = UserId.New;
        var beforeRegistration = DateTimeOffset.UtcNow;
        var command = new RegisterUserCommand(userId)
        {
            Email = "timestamp@example.com",
            Name = "Timestamp Test",
            PasswordHash = "hashed_password_456"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);
        var afterRegistration = DateTimeOffset.UtcNow;

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.RegisteredAt >= beforeRegistration);
        Assert.IsTrue(user.RegisteredAt <= afterRegistration);
    }

    [TestMethod]
    public async Task RegisterUser_MultipleUsers_AllCreatedSuccessfully()
    {
        // Arrange
        var user1Id = UserId.New;
        var user2Id = UserId.New;
        var user3Id = UserId.New;

        var command1 = new RegisterUserCommand(user1Id)
        {
            Email = "user1@example.com",
            Name = "User One",
            PasswordHash = "hash1"
        };

        var command2 = new RegisterUserCommand(user2Id)
        {
            Email = "user2@example.com",
            Name = "User Two",
            PasswordHash = "hash2"
        };

        var command3 = new RegisterUserCommand(user3Id)
        {
            Email = "user3@example.com",
            Name = "User Three",
            PasswordHash = "hash3"
        };

        // Act
        await _commandBus.PublishAsync(command1, CancellationToken.None);
        await _commandBus.PublishAsync(command2, CancellationToken.None);
        await _commandBus.PublishAsync(command3, CancellationToken.None);

        // Assert
        var query = new GetAllUsersQuery();
        var users = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(users);
        Assert.AreEqual(3, users.Count());
        Assert.IsTrue(users.Any(u => u.Email == "user1@example.com"));
        Assert.IsTrue(users.Any(u => u.Email == "user2@example.com"));
        Assert.IsTrue(users.Any(u => u.Email == "user3@example.com"));
    }

    [TestMethod]
    public async Task RegisterUser_CanBeQueriedByEmail()
    {
        // Arrange
        var userId = UserId.New;
        var email = "queryme@example.com";
        var command = new RegisterUserCommand(userId)
        {
            Email = email,
            Name = "Query Test",
            PasswordHash = "hash_query"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetUserByEmailQuery(email);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.AreEqual(userId.Value, user.Id);
        Assert.AreEqual(email, user.Email);
    }
}
