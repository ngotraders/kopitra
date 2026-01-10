using EventFlow.EntityFramework;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace Kopitra.Api.Domain.ExpertAdvisors.Queries;

public class GetSessionByIdQueryHandler : IQueryHandler<GetSessionByIdQuery, ExpertAdvisorSessionReadModel>
{
    private readonly IDbContextProvider<KopitraDbContext> _contextProvider;

    public GetSessionByIdQueryHandler(IDbContextProvider<KopitraDbContext> contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<ExpertAdvisorSessionReadModel> ExecuteQueryAsync(GetSessionByIdQuery query, CancellationToken cancellationToken)
    {
        using var context = _contextProvider.CreateContext();
        var result = await context.ExpertAdvisorSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken)
            .ConfigureAwait(false);
        
        if (result == null)
        {
            throw new InvalidOperationException($"Session {query.SessionId} not found");
        }
        
        return result;
    }
}

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
