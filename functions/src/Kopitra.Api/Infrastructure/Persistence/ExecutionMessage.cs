namespace Kopitra.Api.Infrastructure.Persistence;

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
