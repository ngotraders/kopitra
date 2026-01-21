using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Query to get all users with optional search filter
/// </summary>
public class GetAllUsersQuery : IQuery<IEnumerable<UserReadModel>>
{
    public string? SearchTerm { get; }

    public GetAllUsersQuery(string? searchTerm = null)
    {
        SearchTerm = searchTerm;
    }
}
