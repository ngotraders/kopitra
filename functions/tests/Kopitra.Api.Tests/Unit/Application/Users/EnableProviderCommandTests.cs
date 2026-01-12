using EventFlow;
using EventFlow.Queries;
using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Application.Users.Queries;
using Kopitra.Api.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Kopitra.Api.Tests.Unit.Application.Users;

[TestClass]
public class EnableProviderCommandTests
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
    public async Task EnableProvider_WithValidData_EnablesProviderRoleSuccessfully()
    {
        // Arrange - Create user first
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "provider@example.com",
            DisplayName = "Provider Test",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new EnableProviderCommand(userId)
        {
            ProviderDescription = "Expert FX Trader with 10 years experience"
        };

        // Act
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Assert - Verify ReadModel
        var query = new GetUserByIdQuery(userId);
        var user = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.CanProvide);
        Assert.IsTrue(user.Roles.Contains("Provider"));
    }

    [TestMethod]
    public async Task EnableProvider_WhenAlreadyEnabled_ThrowsException()
    {
        // Arrange - Create user and enable provider
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "double@example.com",
            DisplayName = "Double Enable",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command1 = new EnableProviderCommand(userId)
        {
            ProviderDescription = "First time"
        };
        await _commandBus.PublishAsync(command1, CancellationToken.None);

        var command2 = new EnableProviderCommand(userId)
        {
            ProviderDescription = "Second time"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await _commandBus.PublishAsync(command2, CancellationToken.None);
        });
    }

    [TestMethod]
    public async Task EnableProvider_UserCanBeQueriedByProviderRole()
    {
        // Arrange - Create user and enable provider
        var userId = UserId.New;
        var registerCommand = new RegisterUserCommand(userId)
        {
            Email = "queryProvider@example.com",
            DisplayName = "Query Provider",
            PasswordHash = "hash"
        };
        await _commandBus.PublishAsync(registerCommand, CancellationToken.None);

        var command = new EnableProviderCommand(userId)
        {
            ProviderDescription = "Test Provider"
        };
        await _commandBus.PublishAsync(command, CancellationToken.None);

        // Act
        var query = new GetUsersByRoleQuery("Provider");
        var providers = await _queryProcessor.ProcessAsync(query, CancellationToken.None);

        // Assert
        Assert.IsNotNull(providers);
        Assert.IsTrue(providers.Any(p => p.Id == userId.Value));
    }
}
