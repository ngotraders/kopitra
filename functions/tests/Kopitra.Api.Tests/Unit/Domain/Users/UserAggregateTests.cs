using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.Users.Events;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Domain.Users;

[TestClass]
public class UserAggregateTests
{
    #region Construction Tests

    [TestMethod]
    public void UserAggregate_CanBeInstantiated()
    {
        // Arrange & Act
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id);
    }

    [TestMethod]
    public void UserAggregate_IsInitiallyActive()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Assert
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void UserAggregate_IsInitiallyNotProvider()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Assert
        Assert.IsFalse(user.IsProviderEnabled);
    }

    #endregion

    #region Register Tests

    [TestMethod]
    public void Register_WithValidData_EmitsUserRegisteredEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var email = "test@example.com";
        var displayName = "Test User";
        var passwordHash = "hashed_password_123";

        // Act
        user.Register(email, displayName, passwordHash);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserRegisteredEvent));

        var evt = (UserRegisteredEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(email, evt.Email);
        Assert.AreEqual(displayName, evt.DisplayName);
        Assert.AreEqual(passwordHash, evt.PasswordHash);
        Assert.IsNotNull(evt.Roles);
        Assert.IsTrue(evt.Roles.Contains("Subscriber"));
    }

    [TestMethod]
    public void Register_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var email = "test@example.com";
        var displayName = "Test User";
        var passwordHash = "hashed_password_123";

        // Act
        user.Register(email, displayName, passwordHash);

        // Assert
        Assert.AreEqual(email, user.Email);
        Assert.AreEqual(displayName, user.DisplayName);
        Assert.AreEqual(passwordHash, user.PasswordHash);
        Assert.IsTrue(user.Roles.Contains("Subscriber"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.Register("", "Test User", "hashed_password");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyDisplayName_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.Register("test@example.com", "", "hashed_password");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Register_WithEmptyPasswordHash_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.Register("test@example.com", "Test User", "");
    }

    #endregion

    #region RecordLogin Tests

    [TestMethod]
    public void RecordLogin_WhenUserActive_EmitsUserLoginEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");

        // Act
        user.RecordLogin();

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(1).ToList(); // Skip Register event
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserLoginEvent));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void RecordLogin_WhenUserInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.Deactivate("Test deactivation");

        // Act
        user.RecordLogin();
    }

    #endregion

    #region UpdateSettings Tests

    [TestMethod]
    public void UpdateSettings_WithValidSettings_EmitsUserSettingsUpdatedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var settings = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "language", "en" }
        };

        // Act
        user.UpdateSettings(settings);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserSettingsUpdatedEvent));

        var evt = (UserSettingsUpdatedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(settings, evt.Settings);
    }

    [TestMethod]
    public void UpdateSettings_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var settings = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "language", "en" }
        };

        // Act
        user.UpdateSettings(settings);

        // Assert
        Assert.AreEqual(settings, user.Settings);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateSettings_WithNullSettings_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.UpdateSettings(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateSettings_WithEmptySettings_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.UpdateSettings(new Dictionary<string, object>());
    }

    #endregion

    #region EnableProvider Tests

    [TestMethod]
    public void EnableProvider_WhenNotEnabled_EmitsUserProviderRoleEnabledEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var description = "Expert FX trader";

        // Act
        user.EnableProvider(description);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserProviderRoleEnabledEvent));

        var evt = (UserProviderRoleEnabledEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(description, evt.ProviderDescription);
    }

    [TestMethod]
    public void EnableProvider_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");


        // Act
        user.EnableProvider("Expert FX trader");

        // Assert
        Assert.IsTrue(user.IsProviderEnabled);
        Assert.IsTrue(user.Roles.Contains("Provider"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EnableProvider_WhenAlreadyEnabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.EnableProvider("First time");


        // Act
        user.EnableProvider("Second time");
    }

    #endregion

    #region DisableProvider Tests

    [TestMethod]
    public void DisableProvider_WhenEnabled_EmitsUserProviderRoleDisabledEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.EnableProvider("Expert FX trader");


        // Act
        user.DisableProvider(DateTime.UtcNow);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(2).ToList(); // Skip Register and EnableProvider events
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserProviderRoleDisabledEvent));
    }

    [TestMethod]
    public void DisableProvider_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.EnableProvider("Expert FX trader");


        // Act
        user.DisableProvider(DateTime.UtcNow);

        // Assert
        Assert.IsFalse(user.IsProviderEnabled);
        Assert.IsFalse(user.Roles.Contains("Provider"));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void DisableProvider_WhenNotEnabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.DisableProvider(DateTime.UtcNow);
    }

    #endregion

    #region Deactivate Tests

    [TestMethod]
    public void Deactivate_WhenActive_EmitsUserDeactivatedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");

        var reason = "Account violation";

        // Act
        user.Deactivate(reason);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(1).ToList(); // Skip Register event
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserDeactivatedEvent));

        var evt = (UserDeactivatedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(reason, evt.Reason);
    }

    [TestMethod]
    public void Deactivate_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");


        // Act
        user.Deactivate("Account violation");

        // Assert
        Assert.IsFalse(user.IsActive);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Deactivate_WhenAlreadyInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.Deactivate("First time");


        // Act
        user.Deactivate("Second time");
    }

    #endregion

    #region IssueRefreshToken Tests

    [TestMethod]
    public void IssueRefreshToken_WhenActive_EmitsUserRefreshTokenIssuedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");

        var sessionId = "session_123";
        var refreshToken = "refresh_token_123";
        var expiresAt = DateTime.UtcNow.AddDays(30);

        // Act
        user.IssueRefreshToken(sessionId, refreshToken, expiresAt);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(1).ToList(); // Skip Register event
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserRefreshTokenIssuedEvent));

        var evt = (UserRefreshTokenIssuedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(sessionId, evt.SessionId);
        Assert.AreEqual(refreshToken, evt.RefreshToken);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void IssueRefreshToken_WhenInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.Deactivate("Test deactivation");


        // Act
        user.IssueRefreshToken("session_123", "refresh_token_123", DateTime.UtcNow.AddDays(30));
    }

    #endregion

    #region UpdateInfo Tests

    [TestMethod]
    public void UpdateInfo_WithEmail_EmitsUserInfoUpdatedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("old@example.com", "Test User", "hashed_password");

        var newEmail = "new@example.com";

        // Act
        user.UpdateInfo(newEmail, null);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(1).ToList(); // Skip Register event
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserInfoUpdatedEvent));

        var evt = (UserInfoUpdatedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(newEmail, evt.Email);
    }

    [TestMethod]
    public void UpdateInfo_WithDisplayName_EmitsUserInfoUpdatedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Old Name", "hashed_password");

        var newDisplayName = "New Name";

        // Act
        user.UpdateInfo(null, newDisplayName);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(1).ToList(); // Skip Register event
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserInfoUpdatedEvent));

        var evt = (UserInfoUpdatedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(newDisplayName, evt.DisplayName);
    }

    [TestMethod]
    public void UpdateInfo_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("old@example.com", "Old Name", "hashed_password");

        var newEmail = "new@example.com";
        var newDisplayName = "New Name";

        // Act
        user.UpdateInfo(newEmail, newDisplayName);

        // Assert
        Assert.AreEqual(newEmail, user.Email);
        Assert.AreEqual(newDisplayName, user.DisplayName);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateInfo_WithBothNull_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");


        // Act
        user.UpdateInfo(null, null);
    }

    #endregion

    #region ChangePermissions Tests

    [TestMethod]
    public void ChangePermissions_WithCanProvide_EmitsUserPermissionsChangedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.ChangePermissions(canProvide: true, canSubscribe: null);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserPermissionsChangedEvent));

        var evt = (UserPermissionsChangedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(true, evt.CanProvide);
        Assert.IsNull(evt.CanSubscribe);
    }

    [TestMethod]
    public void ChangePermissions_WithCanSubscribe_EmitsUserPermissionsChangedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.ChangePermissions(canProvide: null, canSubscribe: true);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserPermissionsChangedEvent));

        var evt = (UserPermissionsChangedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.IsNull(evt.CanProvide);
        Assert.AreEqual(true, evt.CanSubscribe);
    }

    [TestMethod]
    public void ChangePermissions_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.ChangePermissions(canProvide: true, canSubscribe: null);

        // Assert
        Assert.IsTrue(user.IsProviderEnabled);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ChangePermissions_WithBothNull_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.ChangePermissions(canProvide: null, canSubscribe: null);
    }

    #endregion

    #region Reactivate Tests

    [TestMethod]
    public void Reactivate_WhenInactive_EmitsUserReactivatedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.Deactivate("Test deactivation");


        // Act
        user.Reactivate();

        // Assert
        var uncommittedEvents = user.UncommittedEvents.Skip(2).ToList(); // Skip Register and Deactivate events
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserReactivatedEvent));
    }

    [TestMethod]
    public void Reactivate_AppliesEventCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");
        user.Deactivate("Test deactivation");


        // Act
        user.Reactivate();

        // Assert
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Reactivate_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        user.Register("test@example.com", "Test User", "hashed_password");


        // Act
        user.Reactivate();
    }

    #endregion

    #region RecordAdminImpersonation Tests

    [TestMethod]
    public void RecordAdminImpersonation_WithValidData_EmitsUserAdminImpersonationStartedEvent()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var adminUserId = new UserId($"user-{Guid.NewGuid()}");
        var action = "UpdateSettings";
        var details = new Dictionary<string, object> { { "setting", "theme" } };

        // Act
        user.RecordAdminImpersonation(adminUserId, action, details);

        // Assert
        var uncommittedEvents = user.UncommittedEvents.ToList();
        Assert.AreEqual(1, uncommittedEvents.Count);
        Assert.IsInstanceOfType(uncommittedEvents[0].AggregateEvent, typeof(UserAdminImpersonationStartedEvent));

        var evt = (UserAdminImpersonationStartedEvent)uncommittedEvents[0].AggregateEvent;
        Assert.AreEqual(adminUserId, evt.AdminUserId);
        Assert.AreEqual(action, evt.Action);
        Assert.AreEqual(details, evt.Details);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void RecordAdminImpersonation_WithNullAdminId_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act
        user.RecordAdminImpersonation(null!, "Action", new Dictionary<string, object>());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void RecordAdminImpersonation_WithEmptyAction_ThrowsArgumentException()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);
        var adminUserId = new UserId($"user-{Guid.NewGuid()}");

        // Act
        user.RecordAdminImpersonation(adminUserId, "", new Dictionary<string, object>());
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void UserAggregate_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var user = new UserAggregate(userId);

        // Act & Assert - Register
        user.Register("test@example.com", "Test User", "hashed_password");
        Assert.AreEqual("test@example.com", user.Email);
        Assert.IsTrue(user.IsActive);


        // Act & Assert - Enable Provider
        user.EnableProvider("Expert trader");
        Assert.IsTrue(user.IsProviderEnabled);


        // Act & Assert - Update Info
        user.UpdateInfo("newemail@example.com", "New Name");
        Assert.AreEqual("newemail@example.com", user.Email);
        Assert.AreEqual("New Name", user.DisplayName);


        // Act & Assert - Deactivate
        user.Deactivate("Test reason");
        Assert.IsFalse(user.IsActive);


        // Act & Assert - Reactivate
        user.Reactivate();
        Assert.IsTrue(user.IsActive);


        // Act & Assert - Disable Provider
        user.DisableProvider(DateTime.UtcNow);
        Assert.IsFalse(user.IsProviderEnabled);
    }

    #endregion
}

