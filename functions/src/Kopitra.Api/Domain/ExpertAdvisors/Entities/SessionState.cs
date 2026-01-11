namespace Kopitra.Api.Domain.ExpertAdvisors.Entities;

/// <summary>
/// Expert Advisor session state enumeration
/// </summary>
public enum SessionState
{
    Idle = 0,
    Pending = 1,
    Authenticated = 2,
    Closed = 3
}
