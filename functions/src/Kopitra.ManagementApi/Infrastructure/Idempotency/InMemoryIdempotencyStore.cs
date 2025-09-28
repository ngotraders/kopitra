using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.Idempotency;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<(string TenantId, string Key), string> _records = new();

    public Task<IdempotencyResult> TryStoreAsync(string tenantId, string key, string hash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var recordKey = (tenantId, key);
        if (_records.TryGetValue(recordKey, out var existing))
        {
            if (!string.Equals(existing, hash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Idempotency key reused with different payload.");
            }

            return Task.FromResult(new IdempotencyResult(false));
        }

        _records[recordKey] = hash;
        return Task.FromResult(new IdempotencyResult(true));
    }

    public static string ComputeHash(string body)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(body);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
