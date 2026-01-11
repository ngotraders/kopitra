using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// User aggregate ID
/// </summary>
public class UserId : Identity<UserId>
{
    public UserId(string value) : base(value)
    {
    }
}
