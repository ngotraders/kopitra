using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class IssueRefreshTokenCommandTests
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
    public async Task IssueRefreshToken_WithValidData_IssuesSuccessfully()
    {
        // Arrange - Create userSession first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "token@example.com",
            Name = "Token Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var sessionId = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var command = new IssueRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserSessionBySessionIdQuery(sessionId);
        var userSession = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(userSession);
        Assert.AreEqual(userId.Value, userSession.UserId);
        Assert.AreEqual(sessionId, userSession.SessionId);
        Assert.AreEqual(refreshToken, userSession.RefreshToken);
        Assert.IsNotNull(userSession.RefreshTokenExpiresAt);
    }

    [TestMethod]
    public async Task IssueRefreshToken_CanBeQueriedByToken()
    {
        // Arrange - Create userSession and issue token
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "querytoken@example.com",
            Name = "Query Token",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var sessionId = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var command = new IssueRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Act
        var query = new GetUserSessionByRefreshTokenQuery(refreshToken);
        var userSession = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        // Assert
        Assert.IsNotNull(userSession);
        Assert.AreEqual(userId.Value, userSession.UserId);
        Assert.AreEqual(sessionId, userSession.SessionId);
        Assert.AreEqual(refreshToken, userSession.RefreshToken);
    }

    [TestMethod]
    public async Task IssueRefreshToken_WhenInactive_ThrowsException()
    {
        // Arrange - Create and deactivate userSession
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "inactive@example.com",
            Name = "Inactive User",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var deactivateCommand = new DeactivateUserCommand(userId)
        {
            AdminUserId = UserId.New,
            Reason = "Test"
        };
        await _commandBus.PublishAsync(deactivateCommand, CancellationToken.None);

        var command = new IssueRefreshTokenCommand(userId)
        {
            SessionId = Guid.NewGuid().ToString("N"),
            RefreshToken = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }
}
