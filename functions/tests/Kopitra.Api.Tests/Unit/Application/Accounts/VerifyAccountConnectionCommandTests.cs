using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Accounts;

[TestClass]
public class VerifyAccountConnectionCommandTests
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
    public async Task VerifyAccountConnection_WithSuccessfulConnection_UpdatesSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        
        // First, create an account
        var registerCommand = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        // Verify the connection
        var verifyCommand = new VerifyAccountConnectionCommand(accountId)
        {
            IsConnected = true,
            CurrentBalance = 5000.00m
        };

        // Act
        await _commandBus.PublishAsync(verifyCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsTrue(account.IsConnected);
        Assert.AreEqual(5000.00m, account.Balance);
    }

    [TestMethod]
    public async Task VerifyAccountConnection_WithFailedConnection_UpdatesSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        
        var registerCommand = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var verifyCommand = new VerifyAccountConnectionCommand(accountId)
        {
            IsConnected = false,
            CurrentBalance = 0m
        };

        // Act
        await _commandBus.PublishAsync(verifyCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsFalse(account.IsConnected);
    }

    [TestMethod]
    public async Task VerifyAccountConnection_WithLargeBalance_UpdatesSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        
        var registerCommand = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var largeBalance = 1000000.50m;
        var verifyCommand = new VerifyAccountConnectionCommand(accountId)
        {
            IsConnected = true,
            CurrentBalance = largeBalance
        };

        // Act
        await _commandBus.PublishAsync(verifyCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsTrue(account.IsConnected);
        Assert.AreEqual(largeBalance, account.Balance);
    }

    [TestMethod]
    public async Task VerifyAccountConnection_UpdatesLastVerifiedAtTimestamp()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        
        var registerCommand = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var beforeVerification = DateTime.UtcNow;
        var verifyCommand = new VerifyAccountConnectionCommand(accountId)
        {
            IsConnected = true,
            CurrentBalance = 5000.00m
        };

        // Act
        await _commandBus.PublishAsync(verifyCommand, CancellationToken.None);
        var afterVerification = DateTime.UtcNow;

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsNotNull(account.LastVerifiedAt);
        Assert.IsTrue(account.LastVerifiedAt >= beforeVerification);
        Assert.IsTrue(account.LastVerifiedAt <= afterVerification);
    }
}
