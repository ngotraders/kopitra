namespace Kopitra.ManagementApi.Infrastructure;

public interface IIdempotencyStore<TResponse>
{
    Task<IdempotencyRecord<TResponse>?> TryGetAsync(string scope, string key, CancellationToken cancellationToken);

    Task SaveAsync(string scope, string key, IdempotencyRecord<TResponse> record, CancellationToken cancellationToken);
}
