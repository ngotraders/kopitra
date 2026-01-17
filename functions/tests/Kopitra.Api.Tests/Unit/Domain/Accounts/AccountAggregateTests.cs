using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain.Accounts;

[TestClass]
public class AccountAggregateTests
{
    #region Construction Tests

    [TestMethod]
    public void AccountAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Assert
        Assert.IsNotNull(account);
        Assert.AreEqual(accountId, account.Id);
    }

    [TestMethod]
    public void AccountAggregate_IsInitiallyNotConnected()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Assert
        Assert.IsFalse(account.IsConnected);
        Assert.AreEqual(ConnectionStatus.NotConnected, account.ConnectionStatus);
    }

    [TestMethod]
    public void AccountAggregate_IsInitiallyNotDeleted()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Assert
        Assert.IsFalse(account.IsDeleted);
    }

    #endregion

    #region Register Tests

    [TestMethod]
    public void Register_WithValidData_EmitsAccountRegisteredEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var brokerName = "XM";
        var accountNumber = "12345678";
        var serverName = "XMGlobal-Demo";

        // Act
        account.Register(userId, BrokerType.MT4, brokerName, accountNumber, serverName);

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOfType(events[0].AggregateEvent, typeof(AccountRegisteredEvent));

        var evt = (AccountRegisteredEvent)events[0].AggregateEvent;
        Assert.AreEqual(userId, evt.UserId);
        Assert.AreEqual(BrokerType.MT4, evt.BrokerType);
        Assert.AreEqual(brokerName, evt.BrokerName);
        Assert.AreEqual(accountNumber, evt.AccountNumber);
        Assert.AreEqual(serverName, evt.ServerName);
    }

    [TestMethod]
    public void Register_WithApiCredentials_IncludesInEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var apiKey = "test_key_123";
        var apiSecret = "test_secret_456";

        // Act
        account.Register(userId, BrokerType.MT5, "FXCM", "87654321", "FXCM-Live", apiKey, apiSecret);

        // Assert
        var evt = (AccountRegisteredEvent)account.UncommittedEvents.First().AggregateEvent;
        Assert.AreEqual(apiKey, evt.ApiKey);
        Assert.AreEqual(apiSecret, evt.ApiSecret);
    }

    [TestMethod]
    public void Register_WithNullApiCredentials_AllowsRegistration()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", null, null);

        // Assert
        var evt = (AccountRegisteredEvent)account.UncommittedEvents.First().AggregateEvent;
        Assert.IsNull(evt.ApiKey);
        Assert.IsNull(evt.ApiSecret);
    }

    [TestMethod]
    public void Register_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Assert
        Assert.AreEqual(userId, account.UserId);
        Assert.AreEqual(BrokerType.MT4, account.BrokerType);
        Assert.AreEqual("XM", account.BrokerName);
        Assert.AreEqual("12345678", account.AccountNumber);
        Assert.AreEqual("XMGlobal-Demo", account.ServerName);
        Assert.IsFalse(account.IsConnected);
        Assert.AreEqual(ConnectionStatus.TestPending, account.ConnectionStatus);
        Assert.AreEqual(0, account.Balance);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Register_WithNullUserId_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.Register(null!, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyBrokerName_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.Register(userId, BrokerType.MT4, "", "12345678", "XMGlobal-Demo");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyAccountNumber_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.Register(userId, BrokerType.MT4, "XM", "", "XMGlobal-Demo");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyServerName_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "");
    }

    #endregion

    #region VerifyConnection Tests

    [TestMethod]
    public void VerifyConnection_WithSuccessfulConnection_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.VerifyConnection(true, 10000.50m);

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountConnectionVerifiedEvent));

        var evt = (AccountConnectionVerifiedEvent)events[1].AggregateEvent;
        Assert.IsTrue(evt.IsConnected);
        Assert.AreEqual(10000.50m, evt.CurrentBalance);
    }

    [TestMethod]
    public void VerifyConnection_WithFailedConnection_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.VerifyConnection(false, 0);

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountConnectionVerifiedEvent));

        var evt = (AccountConnectionVerifiedEvent)events[1].AggregateEvent;
        Assert.IsFalse(evt.IsConnected);
        Assert.AreEqual(0, evt.CurrentBalance);
    }

    [TestMethod]
    public void VerifyConnection_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.VerifyConnection(true, 5000.75m);

        // Assert
        Assert.IsTrue(account.IsConnected);
        Assert.AreEqual(5000.75m, account.Balance);
        Assert.AreEqual(ConnectionStatus.Connected, account.ConnectionStatus);
        Assert.IsNotNull(account.LastVerifiedAt);
    }

    [TestMethod]
    public void VerifyConnection_WithFailure_UpdatesConnectionStatus()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.VerifyConnection(false, 0);

        // Assert
        Assert.IsFalse(account.IsConnected);
        Assert.AreEqual(ConnectionStatus.NotConnected, account.ConnectionStatus);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void VerifyConnection_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
        account.Delete();

        // Act
        account.VerifyConnection(true, 5000);
    }

    #endregion

    #region UpdateBalance Tests

    [TestMethod]
    public void UpdateBalance_WithValidBalance_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateBalance(50000.00m);

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountBalanceUpdatedEvent));

        var evt = (AccountBalanceUpdatedEvent)events[1].AggregateEvent;
        Assert.AreEqual(50000.00m, evt.Balance);
    }

    [TestMethod]
    public void UpdateBalance_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateBalance(25000.50m);

        // Assert
        Assert.AreEqual(25000.50m, account.Balance);
        Assert.IsNotNull(account.LastSyncedAt);
    }

    [TestMethod]
    public void UpdateBalance_WithZeroBalance_IsValid()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateBalance(0);

        // Assert
        Assert.AreEqual(0, account.Balance);
    }

    [TestMethod]
    public void UpdateBalance_MultipleUpdates_LastOneWins()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateBalance(10000);
        account.UpdateBalance(20000);
        account.UpdateBalance(30000);

        // Assert
        Assert.AreEqual(30000, account.Balance);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateBalance_WithNegativeBalance_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateBalance(-1000);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void UpdateBalance_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
        account.Delete();

        // Act
        account.UpdateBalance(50000);
    }

    #endregion

    #region UpdateInfo Tests

    [TestMethod]
    public void UpdateInfo_WithAllProperties_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateInfo("87654321", "XMGlobal-Live", "new_api_key", "new_api_secret");

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountInfoUpdatedEvent));

        var evt = (AccountInfoUpdatedEvent)events[1].AggregateEvent;
        Assert.AreEqual("87654321", evt.AccountNumber);
        Assert.AreEqual("XMGlobal-Live", evt.ServerName);
        Assert.AreEqual("new_api_key", evt.ApiKey);
        Assert.AreEqual("new_api_secret", evt.ApiSecret);
    }

    [TestMethod]
    public void UpdateInfo_WithOnlyAccountNumber_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateInfo(accountNumber: "99999999");

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountInfoUpdatedEvent));

        var evt = (AccountInfoUpdatedEvent)events[1].AggregateEvent;
        Assert.AreEqual("99999999", evt.AccountNumber);
        Assert.IsNull(evt.ServerName);
        Assert.IsNull(evt.ApiKey);
        Assert.IsNull(evt.ApiSecret);
    }

    [TestMethod]
    public void UpdateInfo_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateInfo(accountNumber: "11111111", serverName: "XMGlobal-Live");

        // Assert
        Assert.AreEqual("11111111", account.AccountNumber);
        Assert.AreEqual("XMGlobal-Live", account.ServerName);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateInfo_WithNoFields_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.UpdateInfo();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void UpdateInfo_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
        account.Delete();

        // Act
        account.UpdateInfo(accountNumber: "99999999");
    }

    #endregion

    #region InitiateActivation Tests

    [TestMethod]
    public void InitiateActivation_WithValidData_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.InitiateActivation("IC Markets", "99999999", "IC-Live");

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOfType(events[0].AggregateEvent, typeof(AccountActivationInitiatedEvent));

        var evt = (AccountActivationInitiatedEvent)events[0].AggregateEvent;
        Assert.AreEqual("IC Markets", evt.BrokerName);
        Assert.AreEqual("99999999", evt.AccountNumber);
        Assert.AreEqual("IC-Live", evt.ServerName);
    }

    [TestMethod]
    public void InitiateActivation_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.InitiateActivation("Pepperstone", "55555555", "Pepperstone-Live");

        // Assert
        Assert.AreEqual("Pepperstone", account.BrokerName);
        Assert.AreEqual("55555555", account.AccountNumber);
        Assert.AreEqual("Pepperstone-Live", account.ServerName);
        Assert.AreEqual(ConnectionStatus.NotConnected, account.ConnectionStatus);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void InitiateActivation_WithEmptyBrokerName_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.InitiateActivation("", "99999999", "IC-Live");
    }

    #endregion

    #region ConfirmActivation Tests

    [TestMethod]
    public void ConfirmActivation_WithValidData_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.InitiateActivation("XM", "12345678", "XMGlobal-Demo");

        account.ConfirmActivation(userId, BrokerType.MT5);

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountActivationConfirmedEvent));

        var evt = (AccountActivationConfirmedEvent)events[1].AggregateEvent;
        Assert.AreEqual(userId, evt.UserId);
        Assert.AreEqual(BrokerType.MT5, evt.BrokerType);
    }

    [TestMethod]
    public void ConfirmActivation_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.InitiateActivation("XM", "12345678", "XMGlobal-Demo");

        // Act
        account.ConfirmActivation(userId, BrokerType.MT4);

        // Assert
        Assert.AreEqual(userId, account.UserId);
        Assert.AreEqual(BrokerType.MT4, account.BrokerType);
        Assert.AreEqual(ConnectionStatus.TestPending, account.ConnectionStatus);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ConfirmActivation_WithNullUserId_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        account.InitiateActivation("XM", "12345678", "XMGlobal-Demo");

        // Act
        account.ConfirmActivation(null!, BrokerType.MT4);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ConfirmActivation_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
        account.Delete();

        // Act
        account.ConfirmActivation(userId, BrokerType.MT5);
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public void Delete_WithValidAccount_EmitsEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        account.Delete();

        // Assert
        var events = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(AccountDeletedEvent));
    }

    [TestMethod]
    public void Delete_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");

        // Act
        account.Delete();

        // Assert
        Assert.IsTrue(account.IsDeleted);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Delete_WhenAlreadyDeleted_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo");
        account.Delete();

        // Act
        account.Delete();
    }

    #endregion
}
