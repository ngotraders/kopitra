using System.ComponentModel.DataAnnotations;

namespace Kopitra.Api.Models;

public class SessionCreateRequest
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public string AccountId { get; set; } = "";
}

public class SessionDetailResponse
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public int State { get; set; }
    public string? JwtToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class SessionSummaryResponse
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public int State { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class HeartbeatRequest
{
    [Required]
    public string SessionId { get; set; } = "";
}
