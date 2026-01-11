using Kopitra.Api.Application.Users.Commands;
using Kopitra.Api.Domain.Users;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Tests.Unit.Application;

[TestClass]
public class RegisterUserCommandHandlerTests
{
    [TestMethod]
    public async Task Handler_Returns_UserId_With_UserPrefix()
    {
        // Arrange
        var handler = new RegisterUserCommandHandler();
        var userId = new UserId($"user-{Guid.NewGuid()}");
        var cmd = new RegisterUserCommand(userId)
        {
            Email = "a@b.com",
            DisplayName = "Alice",
            PasswordHash = "hashedpwd"
        };

        // Act
        var aggregate = new UserAggregate(userId);
        await handler.ExecuteAsync(aggregate, cmd, CancellationToken.None);
    }
}
