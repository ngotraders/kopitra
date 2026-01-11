using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Account aggregate ID
/// </summary>
public class AccountId : Identity<AccountId>
{
    public AccountId(string value) : base(value)
    {
    }
}
