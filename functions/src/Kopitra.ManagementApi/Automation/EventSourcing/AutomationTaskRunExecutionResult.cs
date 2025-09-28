using EventFlow.Aggregates.ExecutionResults;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

public sealed class AutomationTaskRunExecutionResult : IExecutionResult
{
    public AutomationTaskRunExecutionResult(TaskExecutionSummary summary, AutomationTaskRunResponse response)
    {
        Summary = summary;
        Response = response;
    }

    public TaskExecutionSummary Summary { get; }

    public AutomationTaskRunResponse Response { get; }

    public bool IsSuccess => true;
}
