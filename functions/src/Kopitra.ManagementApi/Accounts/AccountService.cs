using Kopitra.ManagementApi.Infrastructure;

namespace Kopitra.ManagementApi.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly IManagementRepository _repository;

    public AccountService(IManagementRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ManagedAccountSummary>> ListAccountsAsync(string tenantId, CancellationToken cancellationToken)
    {
        return _repository.ListAccountSummariesAsync(tenantId, cancellationToken);
    }

    public Task<ManagedAccount?> GetAccountAsync(string tenantId, string accountId, CancellationToken cancellationToken)
    {
        return _repository.FindAccountAsync(tenantId, accountId, cancellationToken);
    }
}
