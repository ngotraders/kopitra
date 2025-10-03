using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Infrastructure.Sessions;

public sealed class InMemoryExpertAdvisorSessionDirectory : IExpertAdvisorSessionDirectory
{
    private readonly ConcurrentDictionary<string, ExpertAdvisorSessionRecord> _sessions =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly IClock _clock;

    public InMemoryExpertAdvisorSessionDirectory(IClock clock)
    {
        _clock = clock;
    }

    public ValueTask RegisterAsync(ExpertAdvisorSessionRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions.AddOrUpdate(record.AccountId, record, (_, _) => record);
        return ValueTask.CompletedTask;
    }

    public ValueTask<ExpertAdvisorSessionRecord?> GetAsync(string accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_sessions.TryGetValue(accountId, out var record))
        {
            if (!record.IsExpired(_clock.UtcNow))
            {
                return ValueTask.FromResult<ExpertAdvisorSessionRecord?>(record);
            }

            _sessions.TryRemove(accountId, out _);
        }

        return ValueTask.FromResult<ExpertAdvisorSessionRecord?>(null);
    }
}
