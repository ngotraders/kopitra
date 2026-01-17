using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain;

/// <summary>
/// Unit tests for AccountAggregate
/// Tests all domain logic: registration, activation, balance updates, deletion, etc.
/// </summary>
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
    public void AccountAggregate_InitiallyNotDeleted()
    {
        // Arrange & Act
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Assert
        Assert.IsFalse(account.IsDeleted);
    }

    [TestMethod]
    public void AccountAggregate_InitiallyNotConnected()
    {
        // Arrange & Act
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Assert
        Assert.IsFalse(account.IsConnected);
        Assert.AreEqual(ConnectionStatus.NotConnected, account.ConnectionStatus);
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
        var brokerType = BrokerType.MT5;
        var brokerName = "XM Global";
        var accountNumber = "12345678";
        var serverName = "XMGlobal-Demo";

        // Act
        account.Register(userId, brokerType, brokerName, accountNumber, serverName);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(AccountRegisteredEvent));

        var evt = (AccountRegisteredEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(userId, evt.UserId);
        Assert.AreEqual(brokerType, evt.BrokerType);
        Assert.AreEqual(brokerName, evt.BrokerName);
        Assert.AreEqual(accountNumber, evt.AccountNumber);
        Assert.AreEqual(serverName, evt.ServerName);
    }

    [TestMethod]
    public void Register_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var brokerType = BrokerType.MT4;
        var brokerName = "Pepperstone";
        var accountNumber = "87654321";
        var serverName = "Pepperstone-Live";

        // Act
        account.Register(userId, brokerType, brokerName, accountNumber, serverName);

        // Assert
        Assert.AreEqual(userId, account.UserId);
        Assert.AreEqual(brokerType, account.BrokerType);
        Assert.AreEqual(brokerName, account.BrokerName);
        Assert.AreEqual(accountNumber, account.AccountNumber);
        Assert.AreEqual(serverName, account.ServerName);
        Assert.AreEqual(ConnectionStatus.TestPending, account.ConnectionStatus);
        Assert.IsFalse(account.IsConnected);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Register_WithNullUserId_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.Register(null!, BrokerType.MT5, "Broker", "12345", "Server");
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
        account.Register(userId, BrokerType.MT5, "", "12345", "Server");
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
        account.Register(userId, BrokerType.MT5, "Broker", "", "Server");
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
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "");
    }

    #endregion

    #region VerifyConnection Tests

    [TestMethod]
    public void VerifyConnection_WithConnected_EmitsAccountConnectionVerifiedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        
        var balance = 10000m;

        // Act
        account.VerifyConnection(true, balance);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, uncommittedEvents.Count); // Register + VerifyConnection
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountConnectionVerifiedEvent));

        var evt = (AccountConnectionVerifiedEvent)uncommittedEvents[1].AggregateEvent;
        Assert.IsTrue(evt.IsConnected);
        Assert.AreEqual(balance, evt.CurrentBalance);
    }

    [TestMethod]
    public void VerifyConnection_WithConnectedTrue_UpdatesState()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.VerifyConnection(true, 5000m);

        // Assert
        Assert.IsTrue(account.IsConnected);
        Assert.AreEqual(5000m, account.Balance);
        Assert.AreEqual(ConnectionStatus.Connected, account.ConnectionStatus);
        Assert.IsNotNull(account.LastVerifiedAt);
    }

    [TestMethod]
    public void VerifyConnection_WithConnectedFalse_UpdatesState()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.VerifyConnection(false, 0m);

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
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        account.Delete();

        // Act
        account.VerifyConnection(true, 10000m);
    }

    #endregion

    #region InitiateActivation Tests

    [TestMethod]
    public void InitiateActivation_WithValidData_EmitsAccountActivationInitiatedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var brokerName = "FXCM";
        var accountNumber = "11111111";
        var serverName = "FXCM-Demo";

        // Act
        account.InitiateActivation(brokerName, accountNumber, serverName);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(AccountActivationInitiatedEvent));

        var evt = (AccountActivationInitiatedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(brokerName, evt.BrokerName);
        Assert.AreEqual(accountNumber, evt.AccountNumber);
        Assert.AreEqual(serverName, evt.ServerName);
    }

    [TestMethod]
    public void InitiateActivation_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act
        account.InitiateActivation("DukasBank", "22222222", "DukasBank-Demo");

        // Assert
        Assert.AreEqual("DukasBank", account.BrokerName);
        Assert.AreEqual("22222222", account.AccountNumber);
        Assert.AreEqual("DukasBank-Demo", account.ServerName);
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
        account.InitiateActivation("", "12345", "Server");
    }

    #endregion

    #region ConfirmActivation Tests

    [TestMethod]
    public void ConfirmActivation_WithValidData_EmitsAccountActivationConfirmedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        account.InitiateActivation("Broker", "12345", "Server");
        
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var brokerType = BrokerType.MT5;

        // Act
        account.ConfirmActivation(userId, brokerType);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, uncommittedEvents.Count); // InitiateActivation + ConfirmActivation
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountActivationConfirmedEvent));

        var evt = (AccountActivationConfirmedEvent)uncommittedEvents[1].AggregateEvent;
        Assert.AreEqual(userId, evt.UserId);
        Assert.AreEqual(brokerType, evt.BrokerType);
    }

    [TestMethod]
    public void ConfirmActivation_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        account.InitiateActivation("Broker", "12345", "Server");
        
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var brokerType = BrokerType.MT4;

        // Act
        account.ConfirmActivation(userId, brokerType);

        // Assert
        Assert.AreEqual(userId, account.UserId);
        Assert.AreEqual(brokerType, account.BrokerType);
        Assert.AreEqual(ConnectionStatus.TestPending, account.ConnectionStatus);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ConfirmActivation_WithNullUserId_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        account.InitiateActivation("Broker", "12345", "Server");

        // Act
        account.ConfirmActivation(null!, BrokerType.MT5);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ConfirmActivation_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        account.InitiateActivation("Broker", "12345", "Server");
        account.Delete();

        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        account.ConfirmActivation(userId, BrokerType.MT5);
    }

    #endregion

    #region UpdateBalance Tests

    [TestMethod]
    public void UpdateBalance_WithValidBalance_EmitsAccountBalanceUpdatedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        var newBalance = 15000m;

        // Act
        account.UpdateBalance(newBalance);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, uncommittedEvents.Count); // Register + UpdateBalance
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountBalanceUpdatedEvent));

        var evt = (AccountBalanceUpdatedEvent)uncommittedEvents[1].AggregateEvent;
        Assert.AreEqual(newBalance, evt.Balance);
    }

    [TestMethod]
    public void UpdateBalance_AppliesEventCorrectly()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        var newBalance = 25000m;

        // Act
        account.UpdateBalance(newBalance);

        // Assert
        Assert.AreEqual(newBalance, account.Balance);
        Assert.IsNotNull(account.LastSyncedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateBalance_WithNegativeBalance_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.UpdateBalance(-1000m);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void UpdateBalance_OnDeletedAccount_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        account.Delete();

        // Act
        account.UpdateBalance(5000m);
    }

    #endregion

    #region UpdateInfo Tests

    [TestMethod]
    public void UpdateInfo_WithAccountNumber_EmitsAccountInfoUpdatedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.UpdateInfo(accountNumber: "99999999");

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountInfoUpdatedEvent));

        var evt = (AccountInfoUpdatedEvent)uncommittedEvents[1].AggregateEvent;
        Assert.AreEqual("99999999", evt.AccountNumber);
    }

    [TestMethod]
    public void UpdateInfo_WithServerName_UpdatesState()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "OldServer");

        // Act
        account.UpdateInfo(serverName: "NewServer");

        // Assert
        Assert.AreEqual("NewServer", account.ServerName);
    }

    [TestMethod]
    public void UpdateInfo_WithMultipleFields_UpdatesAllFields()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.UpdateInfo(BrokerType.MT4, "NewBroker", "88888888", "NewServer");

        // Assert
        Assert.AreEqual(BrokerType.MT4, account.BrokerType);
        Assert.AreEqual("NewBroker", account.BrokerName);
        Assert.AreEqual("88888888", account.AccountNumber);
        Assert.AreEqual("NewServer", account.ServerName);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateInfo_WithNoFields_ThrowsException()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

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
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        account.Delete();

        // Act
        account.UpdateInfo(serverName: "NewServer");
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public void Delete_WithValidAccount_EmitsAccountDeletedEvent()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");

        // Act
        account.Delete();

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(2, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountDeletedEvent));

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
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        account.Delete();

        // Act
        account.Delete();
    }

    #endregion

    #region State Transition Tests

    [TestMethod]
    public void CompleteFlow_RegisterThroughVerification()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);
        var userId = new UserId($"user-{Guid.NewGuid()}");

        // Act - Register
        account.Register(userId, BrokerType.MT5, "Broker", "12345", "Server");
        Assert.AreEqual(ConnectionStatus.TestPending, account.ConnectionStatus);

        // Act - Verify Connection
        account.VerifyConnection(true, 10000m);
        Assert.IsTrue(account.IsConnected);
        Assert.AreEqual(ConnectionStatus.Connected, account.ConnectionStatus);

        // Act - Update Balance
        account.UpdateBalance(12000m);
        Assert.AreEqual(12000m, account.Balance);

        // Act - Update Info
        account.UpdateInfo(serverName: "NewServer");
        Assert.AreEqual("NewServer", account.ServerName);

        // Assert - All events committed
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(4, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(AccountRegisteredEvent));
        Assert.IsInstanceOfType(uncommittedEvents[1].AggregateEvent, typeof(AccountConnectionVerifiedEvent));
        Assert.IsInstanceOfType(uncommittedEvents[2].AggregateEvent, typeof(AccountBalanceUpdatedEvent));
        Assert.IsInstanceOfType(uncommittedEvents[3].AggregateEvent, typeof(AccountInfoUpdatedEvent));
    }

    [TestMethod]
    public void CompleteFlow_EAInitiatedActivation()
    {
        // Arrange
        var accountId = new AccountId($"account-{Guid.NewGuid()}");
        var account = new AccountAggregate(accountId);

        // Act - Initiate from EA
        account.InitiateActivation("DukasBank", "11111111", "DukasBank-Demo");
        Assert.AreEqual("DukasBank", account.BrokerName);

        // Act - User confirms
        var userId = new UserId($"user-{Guid.NewGuid()}");
        account.ConfirmActivation(userId, BrokerType.MT5);
        Assert.AreEqual(userId, account.UserId);
        Assert.AreEqual(BrokerType.MT5, account.BrokerType);

        // Act - Connection verified
        account.VerifyConnection(true, 20000m);
        Assert.IsTrue(account.IsConnected);

        // Assert
        var uncommittedEvents = account.UncommittedEvents.ToList();
        Assert.AreEqual(3, uncommittedEvents.Count);
    }

    #endregion
}
