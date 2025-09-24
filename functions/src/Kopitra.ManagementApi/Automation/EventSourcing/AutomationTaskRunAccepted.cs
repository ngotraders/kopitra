using System;
using Kopitra.Cqrs.Events;

namespace Kopitra.ManagementApi.Automation.EventSourcing;

public sealed record AutomationTaskRunAccepted(
    string TenantId,
    string TaskId,
    string RunId,
    DateTimeOffset SubmittedAt,
    string Status,
    string Message) : IDomainEvent;
