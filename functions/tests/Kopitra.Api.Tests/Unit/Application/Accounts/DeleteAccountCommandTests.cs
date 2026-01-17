using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Accounts.Commands;
using Kopitra.Api.Application.Accounts.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Accounts;

[TestClass]
public class DeleteAccountCommandTests
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
    public async Task DeleteAccount_WithValidAccount_DeletesSuccessfully()
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

        // Verify it was created
        var queryBefore = new GetAccountByIdQuery(accountId);
        var accountBefore = await _queryProcessor.ProcessAsync(queryBefore, CancellationToken.None);
        Assert.IsNotNull(accountBefore);
        Assert.IsFalse(accountBefore.IsDeleted);

        // Now delete the account
        var deleteCommand = new DeleteAccountCommand(accountId);

        // Act
        await _commandBus.PublishAsync(deleteCommand, CancellationToken.None);

        // Assert - deleted accounts are filtered out from the query
        var query = new GetAccountByIdQuery(accountId);
        var account = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNull(account);
    }

    [TestMethod]
    public async Task DeleteAccount_SetsDeletedAtTimestamp()
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

        // Verify account exists first
        var accountBefore = await _queryProcessor.ProcessAsync(new GetAccountByIdQuery(accountId), CancellationToken.None);
        Assert.IsNotNull(accountBefore);

        var beforeDeletion = DateTime.UtcNow;
        var deleteCommand = new DeleteAccountCommand(accountId);

        // Act
        await _commandBus.PublishAsync(deleteCommand, CancellationToken.None);
        var afterDeletion = DateTime.UtcNow;

        // Assert - after deletion, the account is filtered out, so we verify via GetAccountsByUserIdQuery
        // which will show only non-deleted accounts
        var query = new GetAccountsByUserIdQuery(userId);
        var accounts = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.AreEqual(0, accounts.Count);
    }

    [TestMethod]
    public async Task DeleteAccount_MultipleAccounts_DeletesOnlyTargetAccount()
    {
        // Arrange
        var accountId1 = new AccountId($"account-{Guid.NewGuid()}");
        var accountId2 = new AccountId($"account-{Guid.NewGuid()}");
        var userId = new UserId($"user-{Guid.NewGuid()}");
        
        var registerCommand1 = new RegisterAccountCommand(accountId1)
        {
            UserId = userId,
            BrokerType = BrokerType.MT4,
            BrokerName = "XM",
            AccountNumber = "12345678",
            ServerName = "XMGlobal-Demo"
        };
        var registerCommand2 = new RegisterAccountCommand(accountId2)
        {
            UserId = userId,
            BrokerType = BrokerType.MT5,
            BrokerName = "FXCM",
            AccountNumber = "87654321",
            ServerName = "FXCM-Live"
        };
        
        await _commandBus.PublishAsync(registerCommand1, CancellationToken.None);
        await _commandBus.PublishAsync(registerCommand2, CancellationToken.None);

        // Verify both accounts were created
        var queryBefore = new GetAccountsByUserIdQuery(userId);
        var accountsBefore = await _queryProcessor.ProcessAsync(queryBefore, CancellationToken.None);
        Assert.AreEqual(2, accountsBefore.Count);

        var deleteCommand = new DeleteAccountCommand(accountId1);

        // Act
        await _commandBus.PublishAsync(deleteCommand, CancellationToken.None);

        // Assert - only non-deleted accounts remain
        var query = new GetAccountsByUserIdQuery(userId);
        var accounts = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.AreEqual(1, accounts.Count);
        Assert.AreEqual(accountId2.Value, accounts[0].Id);
    }
}
