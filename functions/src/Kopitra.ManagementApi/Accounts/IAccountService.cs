namespace Kopitra.ManagementApi.Accounts;

public interface IAccountService
{
    Task<IReadOnlyList<ManagedAccountSummary>> ListAccountsAsync(string tenantId, CancellationToken cancellationToken);

    Task<ManagedAccount?> GetAccountAsync(string tenantId, string accountId, CancellationToken cancellationToken);
}
