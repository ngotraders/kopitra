using Kopitra.Api.Domain.Accounts;
using Kopitra.Api.Domain.Accounts.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain.Accounts;

[TestClass]
public class ActivationCodeAggregateTests
{
    #region Construction Tests

    [TestMethod]
    public void ActivationCodeAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);

        // Assert
        Assert.IsNotNull(code);
        Assert.AreEqual(codeId, code.Id);
    }

    [TestMethod]
    public void ActivationCodeAggregate_IsInitiallyPending()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);

        // Assert
        Assert.AreEqual(ActivationCodeStatus.Pending, code.Status);
    }

    #endregion

    #region Generate Tests

    [TestMethod]
    public void Generate_WithValidData_EmitsActivationCodeGeneratedEvent()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var activationCode = "ABC123DEF456";
        var brokerName = "XM";
        var accountNumber = "12345678";
        var serverName = "XMGlobal-Demo";
        var userId = "user-123";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate(activationCode, brokerName, accountNumber, serverName, userId, expiresAt);

        // Assert
        var events = code.UncommittedEvents.ToList();
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOfType(events[0].AggregateEvent, typeof(ActivationCodeGeneratedEvent));

        var evt = (ActivationCodeGeneratedEvent)events[0].AggregateEvent;
        Assert.AreEqual(activationCode, evt.Code);
        Assert.AreEqual(brokerName, evt.BrokerName);
        Assert.AreEqual(accountNumber, evt.AccountNumber);
        Assert.AreEqual(serverName, evt.ServerName);
        Assert.AreEqual(userId, evt.UserId);
    }

    [TestMethod]
    public void Generate_AppliesEventCorrectly()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("XYZ789", "FXCM", "87654321", "FXCM-Live", "user-456", expiresAt);

        // Assert
        Assert.AreEqual("XYZ789", code.Code);
        Assert.AreEqual("FXCM", code.BrokerName);
        Assert.AreEqual("87654321", code.AccountNumber);
        Assert.AreEqual("FXCM-Live", code.ServerName);
        Assert.AreEqual("user-456", code.UserId);
        Assert.AreEqual(ActivationCodeStatus.Pending, code.Status);
        Assert.IsNotNull(code.GeneratedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyCode_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyBrokerName_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", "", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyAccountNumber_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", "XM", "", "XMGlobal-Demo", "user-123", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyServerName_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", "XM", "12345678", "", "user-123", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyUserId_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithPastExpirationTime_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithCurrentTimeAsExpiration_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow;

        // Act
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
    }

    #endregion

    #region Confirm Tests

    [TestMethod]
    public void Confirm_WithValidCode_EmitsActivationCodeConfirmedEvent()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Act
        code.Confirm(BrokerType.MT4);

        // Assert
        var events = code.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(ActivationCodeConfirmedEvent));

        var evt = (ActivationCodeConfirmedEvent)events[1].AggregateEvent;
        Assert.AreEqual("user-123", evt.UserId);
        Assert.AreEqual(BrokerType.MT4, evt.BrokerType);
    }

    [TestMethod]
    public void Confirm_AppliesEventCorrectly()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Act
        code.Confirm(BrokerType.MT5);

        // Assert
        Assert.AreEqual(ActivationCodeStatus.Confirmed, code.Status);
        Assert.IsNotNull(code.ConfirmedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Confirm_WhenAlreadyConfirmed_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        code.Confirm(BrokerType.MT4);

        // Act
        code.Confirm(BrokerType.MT5);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Confirm_OnExpiredCode_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddSeconds(1);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        
        // Wait for expiration
        System.Threading.Thread.Sleep(1100);

        // Act
        code.Confirm(BrokerType.MT4);
    }

    #endregion

    #region Expire Tests

    [TestMethod]
    public void Expire_OnPendingCode_EmitsActivationCodeExpiredEvent()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Act
        code.Expire();

        // Assert
        var events = code.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(ActivationCodeExpiredEvent));
    }

    [TestMethod]
    public void Expire_AppliesEventCorrectly()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Act
        code.Expire();

        // Assert
        Assert.AreEqual(ActivationCodeStatus.Expired, code.Status);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Expire_WhenAlreadyExpired_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        code.Expire();

        // Act
        code.Expire();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Expire_OnConfirmedCode_ThrowsException()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        code.Confirm(BrokerType.MT4);

        // Act
        code.Expire();
    }

    #endregion

    #region IsValid Tests

    [TestMethod]
    public void IsValid_OnPendingCode_ReturnsTrue()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Act
        var isValid = code.IsValid();

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValid_OnConfirmedCode_ReturnsFalse()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        code.Confirm(BrokerType.MT4);

        // Act
        var isValid = code.IsValid();

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void IsValid_OnExpiredCode_ReturnsFalse()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);
        code.Expire();

        // Act
        var isValid = code.IsValid();

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void IsValid_OnPastExpirationTime_ReturnsFalse()
    {
        // Arrange
        var codeId = new ActivationCodeId($"activationcode-{Guid.NewGuid()}");
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddSeconds(1);
        code.Generate("ABC123", "XM", "12345678", "XMGlobal-Demo", "user-123", expiresAt);

        // Wait for expiration
        System.Threading.Thread.Sleep(1100);

        // Act
        var isValid = code.IsValid();

        // Assert
        Assert.IsFalse(isValid);
    }

    #endregion
}
