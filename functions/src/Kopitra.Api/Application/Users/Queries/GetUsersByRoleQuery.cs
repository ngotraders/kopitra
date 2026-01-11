using EventFlow.Queries;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Query to get users by role
/// </summary>
public class GetUsersByRoleQuery : IQuery<IEnumerable<UserReadModel>>
{
    public string Role { get; }

    public GetUsersByRoleQuery(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty.", nameof(role));
        Role = role;
    }
}
