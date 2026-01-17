namespace Kopitra.Api.Functions.Accounts.Models;

/// <summary>
/// Response model for activation code
/// </summary>
public class ActivationCodeResponse
{
    public string ActivationCodeId { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string? UserId { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}
