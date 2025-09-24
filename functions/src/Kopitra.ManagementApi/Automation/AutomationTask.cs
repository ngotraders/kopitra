using System.Collections.Generic;

namespace Kopitra.ManagementApi.Automation;

public sealed record AutomationTask(
    string TaskId,
    string DisplayName,
    string Category,
    string Description,
    bool Enabled,
    TaskSchedule Schedule,
    IReadOnlyDictionary<string, string> Parameters,
    TaskExecutionSummary LastExecution);
