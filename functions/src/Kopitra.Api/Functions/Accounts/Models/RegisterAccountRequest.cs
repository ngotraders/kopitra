namespace Kopitra.Api.Functions.Accounts.Models;

public class RegisterAccountRequest
{
    public string UserId { get; set; } = null!;
    public string BrokerType { get; set; } = null!;
    public string BrokerName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ServerName { get; set; } = null!;
}
