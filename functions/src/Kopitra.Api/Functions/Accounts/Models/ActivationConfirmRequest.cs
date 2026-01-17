namespace Kopitra.Api.Functions.Accounts.Models;

/// <summary>
/// Request model for activating an account with code or account info
/// </summary>
public class ActivationConfirmRequest
{
    public string? ActivationCode { get; set; }
}
