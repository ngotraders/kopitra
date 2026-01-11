using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;
using Kopitra.Api.Domain;
using Kopitra.Api.Domain.ExpertAdvisors;

namespace Kopitra.Api.Application.ExpertAdvisors.Queries;

public class GetAllSessionsQueryHandler : IQueryHandler<GetAllSessionsQuery, IReadOnlyCollection<ExpertAdvisorSessionReadModel>>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetAllSessionsQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<IReadOnlyCollection<ExpertAdvisorSessionReadModel>> ExecuteQueryAsync(GetAllSessionsQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var result = await context.ExpertAdvisorSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.AsReadOnly();
    }
}
