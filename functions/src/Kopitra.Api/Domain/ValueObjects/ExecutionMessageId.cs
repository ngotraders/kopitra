using EventFlow.Core;

namespace Kopitra.Api.Domain.ValueObjects;

/// <summary>
/// Execution message ID
/// </summary>
public class ExecutionMessageId : Identity<ExecutionMessageId>
{
    public ExecutionMessageId(string value) : base(value)
    {
    }
}
