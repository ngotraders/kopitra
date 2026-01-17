namespace Kopitra.Api.Functions.Accounts.Models;

public class VerifyAccountRequest
{
    public bool IsConnected { get; set; }
    public decimal? CurrentBalance { get; set; }
}