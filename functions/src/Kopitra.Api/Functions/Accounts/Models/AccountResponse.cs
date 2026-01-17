namespace Kopitra.Api.Functions.Accounts.Models;

/// <summary>
/// Response model for account information
/// </summary>
public class AccountResponse
{
    public string AccountId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public int BrokerType { get; set; }
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public int ConnectionStatus { get; set; }
    public decimal Balance { get; set; }
    public string? ApiKey { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset? LastConnectionTestAt { get; set; }
    public bool IsDeleted { get; set; }
}
