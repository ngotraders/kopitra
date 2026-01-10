namespace Kopitra.Api.Domain;

/// <summary>
/// Expert Advisor session state
/// </summary>
public enum SessionState
{
    Idle = 0,
    Pending = 1,
    Authenticated = 2,
    Closed = 3
}

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

/// <summary>
/// Signal message from provider
/// </summary>
public class SignalMessage
{
    public string SignalId { get; set; } = null!;
    public string ProviderId { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public int Action { get; set; } // 0: Open, 1: Close, 2: Modify
    public decimal Lot { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Execution message from subscriber EA
/// </summary>
public class ExecutionMessage
{
    public string ExecutionId { get; set; } = null!;
    public string SignalId { get; set; } = null!;
    public string SubscriberId { get; set; } = null!;
    public string AccountId { get; set; } = null!;
    public int OrderTicket { get; set; }
    public int ExecutionStatus { get; set; } // 0: Pending, 1: Executed, 2: Rejected
    public decimal ExecutedLot { get; set; }
    public decimal ExecutionPrice { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public bool Acknowledged { get; set; }
}

/// <summary>
/// Domain event for event sourcing
/// </summary>
public class DomainEvent
{
    public int EventId { get; set; }
    public string AggregateId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }
}
