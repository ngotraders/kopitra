using EventFlow.Queries;
using Kopitra.Api.Domain.ValueObjects;

namespace Kopitra.Api.Application.Users.Queries;

/// <summary>
/// Query to get all users
/// </summary>
public class GetAllUsersQuery : IQuery<IEnumerable<UserReadModel>>
{
}
