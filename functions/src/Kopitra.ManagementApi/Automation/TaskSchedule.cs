namespace Kopitra.ManagementApi.Automation;

public sealed record TaskSchedule(TaskScheduleType Type, string? Expression, TimeSpan? Interval, bool AllowAdHoc);
