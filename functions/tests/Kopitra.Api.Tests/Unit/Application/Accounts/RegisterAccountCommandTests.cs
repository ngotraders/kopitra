using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Accounts;

[TestClass]
public class RegisterAccountCommandTests
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
    public async Task RegisterAccount_WithValidData_CreatesAccountSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var command = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo",
            ApiKey = "test-key",
            ApiSecret = "test-secret"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual(accountId.Value, account.Id);
        Assert.AreEqual(userId.Value, account.UserId);
        Assert.AreEqual((int)BrokerType.MT4, account.BrokerType);
        Assert.AreEqual("XM", account.BrokerName);
        Assert.AreEqual("12345678", account.AccountNumber);
        Assert.AreEqual("XMGlobal-Demo", account.ServerName);
        Assert.AreEqual("test-key", account.ApiKey);
        Assert.AreEqual("test-secret", account.ApiSecret);
        Assert.AreEqual(0m, account.Balance);
        Assert.IsFalse(account.IsConnected);
        Assert.IsFalse(account.IsDeleted);
    }

    [TestMethod]
    public async Task RegisterAccount_WithMT5Broker_CreatesSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var command = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT5,
            BrokerName = "FXCM",
            AccountNumber = "87654321",
            ServerName = "FXCM-Live",
            ApiKey = "fxcm-key",
            ApiSecret = "fxcm-secret"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.AreEqual((int)BrokerType.MT5, account.BrokerType);
        Assert.AreEqual("FXCM", account.BrokerName);
        Assert.AreEqual("87654321", account.AccountNumber);
        Assert.AreEqual("FXCM-Live", account.ServerName);
    }

    [TestMethod]
    public async Task RegisterAccount_WithNullApiCredentials_CreatesSuccessfully()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var command = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo",
            ApiKey = null,
            ApiSecret = null
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsNull(account.ApiKey);
        Assert.IsNull(account.ApiSecret);
    }

    [TestMethod]
    public async Task RegisterAccount_SetsCreatedAtTimestamp()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var command = new RegisterAccountCommand(accountId)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(account);
        Assert.IsTrue(account.CreatedAt >= beforeCreation && account.CreatedAt <= afterCreation);
    }
}
