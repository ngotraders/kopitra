namespace Kopitra.Api.Application.Accounts.Queries;

/// <summary>
/// Read model for ActivationCode queries
/// </summary>
public class ActivationCodeReadModel
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public int Status { get; set; } = 0; // Pending = 0, Confirmed = 1, Expired = 2
    public DateTime ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
