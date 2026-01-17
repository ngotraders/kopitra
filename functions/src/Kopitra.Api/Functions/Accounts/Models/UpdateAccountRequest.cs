namespace Kopitra.Api.Functions.Accounts.Models;

public class UpdateAccountRequest
{
    public string? BrokerType { get; set; }
    public string? BrokerName { get; set; }
    public string? AccountNumber { get; set; }
    public string? ServerName { get; set; }
}