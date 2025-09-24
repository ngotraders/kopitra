using System.Collections.Concurrent;
using Kopitra.ManagementApi.Time;

namespace Kopitra.ManagementApi.Infrastructure;

public sealed class InMemoryIdempotencyStore<TResponse> : IIdempotencyStore<TResponse>
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IdempotencyRecord<TResponse>>> _store;
    private readonly TimeSpan _timeToLive;
    private readonly IClock _clock;

    public InMemoryIdempotencyStore(TimeSpan timeToLive, IClock clock)
    {
        _timeToLive = timeToLive;
        _clock = clock;
        _store = new ConcurrentDictionary<string, ConcurrentDictionary<string, IdempotencyRecord<TResponse>>>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<IdempotencyRecord<TResponse>?> TryGetAsync(string scope, string key, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(scope, out var scopeEntries) && scopeEntries.TryGetValue(key, out var record))
        {
            if (IsExpired(record))
            {
                scopeEntries.TryRemove(key, out _);
                return Task.FromResult<IdempotencyRecord<TResponse>?>(null);
            }

            return Task.FromResult<IdempotencyRecord<TResponse>?>(record);
        }

        return Task.FromResult<IdempotencyRecord<TResponse>?>(null);
    }

    public Task SaveAsync(string scope, string key, IdempotencyRecord<TResponse> record, CancellationToken cancellationToken)
    {
        var scopeEntries = _store.GetOrAdd(scope, _ => new ConcurrentDictionary<string, IdempotencyRecord<TResponse>>(StringComparer.Ordinal));
        scopeEntries[key] = record;

        CleanupExpired(scopeEntries);
        return Task.CompletedTask;
    }

    private bool IsExpired(IdempotencyRecord<TResponse> record)
    {
        return record.CreatedAt.Add(_timeToLive) <= _clock.UtcNow;
    }

    private void CleanupExpired(ConcurrentDictionary<string, IdempotencyRecord<TResponse>> scopeEntries)
    {
        foreach (var entry in scopeEntries)
        {
            if (IsExpired(entry.Value))
            {
                scopeEntries.TryRemove(entry.Key, out _);
            }
        }
    }
}
