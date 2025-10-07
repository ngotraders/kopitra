using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Kopitra.ManagementApi.Infrastructure.AdminUsers;

public sealed class InMemoryAdminUserCredentialStore : IAdminUserCredentialStore
{
    private readonly ConcurrentDictionary<(string TenantId, string Email), AdminUserCredential> _store = new();

    public Task SetAsync(AdminUserCredential credential, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = (TenantId: credential.TenantId, Email: NormalizeEmail(credential.Email));
        _store[key] = credential with { Email = key.Email };
        return Task.CompletedTask;
    }

    public Task<AdminUserCredential?> GetAsync(string tenantId, string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = (TenantId: tenantId, Email: NormalizeEmail(email));
        _store.TryGetValue(key, out var credential);
        return Task.FromResult(credential);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
