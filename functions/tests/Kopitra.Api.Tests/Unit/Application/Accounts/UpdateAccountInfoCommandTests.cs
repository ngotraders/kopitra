using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Accounts;

[TestClass]
public class UpdateAccountInfoCommandTests
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
    public async Task UpdateAccountInfo_WithAllProperties_UpdatesSuccessfully()
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

        // Now update the account info
        var updateCommand = new UpdateAccountInfoCommand(accountId)
        {
            AccountNumber = "87654321",
            ServerName = "XMGlobal-Live",
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual("87654321", account.AccountNumber);
        Assert.AreEqual("XMGlobal-Live", account.ServerName);
    }

    [TestMethod]
    public async Task UpdateAccountInfo_WithOnlyAccountNumber_UpdatesSuccessfully()
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
            ServerName = "XMGlobal-Demo",
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var updateCommand = new UpdateAccountInfoCommand(accountId)
        {
            AccountNumber = "99999999"
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual("99999999", account.AccountNumber);
    }

    [TestMethod]
    public async Task UpdateAccountInfo_WithOnlyServerName_UpdatesSuccessfully()
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

        var updateCommand = new UpdateAccountInfoCommand(accountId)
        {
            ServerName = "XMGlobal-Demo-2"
        };

        // Act
        await _commandBus.PublishAsync(updateCommand, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual("XMGlobal-Demo-2", account.ServerName);
    }
}
