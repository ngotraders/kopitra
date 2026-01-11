namespace Kopitra.Api.Domain.ExpertAdvisors.Entities;

/// <summary>
/// Expert Advisor session entity
/// </summary>
public class ExpertAdvisorSession
{
    public string SessionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public SessionState State { get; set; }
    public string? JwtToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
