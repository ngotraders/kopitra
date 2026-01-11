using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Query to get user by ID
/// </summary>
public class GetUserByIdQuery : IQuery<UserReadModel>
{
    public UserId UserId { get; }

    public GetUserByIdQuery(UserId userId)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
    }
}
