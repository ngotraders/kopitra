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
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);

        // Assert
        Assert.IsNotNull(code);
        Assert.AreEqual(codeId, code.Id);
    }

    [TestMethod]
    public void ActivationCodeAggregate_IsInitiallyPending()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
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
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var activationCode = "ABC123DEF456";
        var brokerType = BrokerType.MT4;
        var brokerName = "XM";
        var accountNumber = "12345678";
        var serverName = "XMGlobal-Demo";
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

        // Act
        code.Generate(activationCode, brokerType, brokerName, accountNumber, serverName, userId, expiresAt);

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
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("XYZ789", BrokerType.MT4, "FXCM", "87654321", "FXCM-Live", userId, expiresAt);

        // Assert
        Assert.AreEqual("XYZ789", code.Code);
        Assert.AreEqual(BrokerType.MT4, code.BrokerType);
        Assert.AreEqual("FXCM", code.BrokerName);
        Assert.AreEqual("87654321", code.AccountNumber);
        Assert.AreEqual("FXCM-Live", code.ServerName);
        Assert.AreEqual(userId, code.UserId);
        Assert.AreEqual(ActivationCodeStatus.Pending, code.Status);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyCode_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", null, expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithInvalidBrokerType_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", (BrokerType)0, "", "12345678", "XMGlobal-Demo", null, expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyBrokerName_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", BrokerType.MT4, "", "12345678", "XMGlobal-Demo", null, expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyAccountNumber_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", BrokerType.MT4, "XM", "", "XMGlobal-Demo", null, expiresAt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Generate_WithEmptyServerName_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Act
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "", null, expiresAt);
    }

    #endregion

    #region Confirm Tests

    [TestMethod]
    public void Confirm_WithValidCode_EmitsActivationCodeConfirmedEvent()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var confirmedAt = DateTimeOffset.UtcNow;
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", null, expiresAt);

        // Act
        code.Confirm(userId, confirmedAt);

        // Assert
        var events = code.UncommittedEvents.ToList();
        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOfType(events[1].AggregateEvent, typeof(ActivationCodeConfirmedEvent));

        var evt = (ActivationCodeConfirmedEvent)events[1].AggregateEvent;
        Assert.AreEqual(userId, evt.UserId);
        Assert.AreEqual(confirmedAt, evt.ConfirmedAt);
    }

    [TestMethod]
    public void Confirm_AppliesEventCorrectly()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var confirmedAt = DateTimeOffset.UtcNow;
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", null, expiresAt);

        // Act
        code.Confirm(userId, confirmedAt);

        // Assert
        Assert.AreEqual(ActivationCodeStatus.Confirmed, code.Status);
        Assert.AreEqual(userId, code.UserId);
        Assert.IsNotNull(code.ConfirmedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Confirm_WhenAlreadyConfirmed_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var confirmedAt = DateTimeOffset.UtcNow;
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);
        code.Confirm(userId, confirmedAt);

        // Act
        code.Confirm(userId, confirmedAt);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Confirm_OnExpiredCode_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow;
        var confirmedAt = DateTimeOffset.UtcNow.AddSeconds(1);
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);

        // Act
        code.Confirm(userId, confirmedAt);
    }

    #endregion

    #region Expire Tests

    [TestMethod]
    public void Expire_OnPendingCode_EmitsActivationCodeExpiredEvent()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);

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
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);

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
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);
        code.Expire();

        // Act
        code.Expire();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Expire_OnConfirmedCode_ThrowsException()
    {
        // Arrange
        var codeId = ActivationCodeId.New;
        var code = new ActivationCodeAggregate(codeId);
        var userId = UserId.New;
        var confirmedAt = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        code.Generate("ABC123", BrokerType.MT4, "XM", "12345678", "XMGlobal-Demo", userId, expiresAt);
        code.Confirm(userId, confirmedAt);

        // Act
        code.Expire();
    }

    #endregion
}
