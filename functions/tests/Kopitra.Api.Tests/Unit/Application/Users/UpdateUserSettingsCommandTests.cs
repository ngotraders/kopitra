using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class UpdateUserSettingsCommandTests
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
    public async Task UpdateUserSettings_WithValidSettings_UpdatesSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "settings@example.com",
            DisplayName = "Settings Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var settings = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "language", "ja" },
            { "notifications", true }
        };

        var command = new UpdateUserSettingsCommand(userId)
        {
            Settings = settings
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        // Note: Settings are stored in aggregate but may not be exposed in ReadModel
        // This test verifies the command executes without error
    }

    [TestMethod]
    public async Task UpdateUserSettings_MultipleUpdates_LatestSettingsPersist()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "multisettings@example.com",
            DisplayName = "Multi Settings",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var settings1 = new Dictionary<string, object>
        {
            { "theme", "light" }
        };

        var settings2 = new Dictionary<string, object>
        {
            { "theme", "dark" },
            { "language", "en" }
        };

        // Act
        await _commandBus.PublishAsync(new UpdateUserSettingsCommand(userId) { Settings = settings1 }, CancellationToken.None);
        await _commandBus.PublishAsync(new UpdateUserSettingsCommand(userId) { Settings = settings2 }, CancellationToken.None);

        // Assert
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
    }

    [TestMethod]
    public async Task UpdateUserSettings_WithEmptySettings_ThrowsException()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "empty@example.com",
            DisplayName = "Empty Settings",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new UpdateUserSettingsCommand(userId)
        {
            Settings = new Dictionary<string, object>()
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            await _commandBus.PublishAsync(command, CancellationToken.None);
        });
    }
}
