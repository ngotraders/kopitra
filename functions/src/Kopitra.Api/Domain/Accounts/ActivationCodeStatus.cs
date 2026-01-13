namespace Kopitra.Api.Domain.Accounts;

/// <summary>
/// Status of an activation code
/// </summary>
public enum ActivationCodeStatus
{
    Pending,
    Confirmed,
    Expired
}