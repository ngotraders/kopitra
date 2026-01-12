using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class RevokeRefreshTokenCommandTests
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
    public async Task RevokeRefreshToken_WithValidData_IssuesSuccessfully()
    {
        // Arrange - Create userSession first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "token@example.com",
            DisplayName = "Token Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var sessionId = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var issueRefreshTokenCommand = new IssueRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        await _commandBus.PublishAsync(issueRefreshTokenCommand, CancellationToken.None);

        var command = new RevokeRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserSessionBySessionIdQuery(sessionId);
        var userSession = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNull(userSession);
    }

    [TestMethod]
    public async Task RevokeRefreshToken_CannotBeQueriedByToken()
    {
        // Arrange - Create userSession and issue token
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "querytoken@example.com",
            DisplayName = "Query Token",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var sessionId = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var issueRefreshTokenCommand = new IssueRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await _commandBus.PublishAsync(issueRefreshTokenCommand, CancellationToken.None);

        var command = new RevokeRefreshTokenCommand(userId)
        {
            SessionId = sessionId,
        };
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Act
        var query = new GetUserSessionByRefreshTokenQuery(refreshToken);
        var userSession = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        // Assert
        Assert.IsNull(userSession);
    }
}
