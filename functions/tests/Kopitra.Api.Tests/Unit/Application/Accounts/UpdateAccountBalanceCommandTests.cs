using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Accounts;

[TestClass]
public class UpdateAccountBalanceCommandTests
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
    public async Task UpdateAccountBalance_WithValidBalance_UpdatesSuccessfully()
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

        // Now update the balance
        var updateCommand = new UpdateAccountBalanceCommand(accountId)
        {
            NewBalance = 5000.50m
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual(5000.50m, account.Balance);
    }

    [TestMethod]
    public async Task UpdateAccountBalance_WithZeroBalance_UpdatesSuccessfully()
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

        var updateCommand = new UpdateAccountBalanceCommand(accountId)
        {
            NewBalance = 0m
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual(0m, account.Balance);
    }

    [TestMethod]
    public async Task UpdateAccountBalance_WithLargeBalance_UpdatesSuccessfully()
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

        var largeBalance = 999999.99m;
        var updateCommand = new UpdateAccountBalanceCommand(accountId)
        {
            NewBalance = largeBalance
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual(largeBalance, account.Balance);
    }

    [TestMethod]
    public async Task UpdateAccountBalance_MultipleUpdates_LastOneWins()
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

        // Update multiple times
        var update1 = new UpdateAccountBalanceCommand(accountId) { NewBalance = 1000m };
        var update2 = new UpdateAccountBalanceCommand(accountId) { NewBalance = 2000m };
        var update3 = new UpdateAccountBalanceCommand(accountId) { NewBalance = 3000m };

        // Act
        await _commandBus.PublishAsync(update1, CancellationToken.None);
        await _commandBus.PublishAsync(update2, CancellationToken.None);
        await _commandBus.PublishAsync(update3, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual(3000m, account.Balance);
    }
}
