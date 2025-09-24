using System.Net;
using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.Admin;

public sealed class RunAutomationTaskFunction
{
    private readonly IAutomationTaskService _taskService;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore<AutomationTaskRunResponse> _idempotencyStore;

    public RunAutomationTaskFunction(
        IAutomationTaskService taskService,
        AdminRequestContextFactory contextFactory,
        IIdempotencyStore<AutomationTaskRunResponse> idempotencyStore)
    {
        _taskService = taskService;
        _contextFactory = contextFactory;
        _idempotencyStore = idempotencyStore;
    }

    [Function("RunAutomationTask")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/tasks/{taskId}/run")] HttpRequestData request,
        string taskId,
        CancellationToken cancellationToken)
    {
        AdminRequestContext context;
        try
        {
            context = _contextFactory.Create(request, requireIdempotencyKey: true);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }

        var scope = FormattableString.Invariant($"tasks/{context.TenantId}/{taskId}");
        var key = context.IdempotencyKey!;

        var existing = await _idempotencyStore.TryGetAsync(scope, key, cancellationToken);
        if (existing is not null)
        {
            return await request.CreateJsonResponseAsync(existing.StatusCode, existing.Response, cancellationToken);
        }

        AutomationTaskRunResponse run;
        try
        {
            run = await _taskService.RunTaskAsync(context.TenantId, taskId, cancellationToken);
        }
        catch (AutomationTaskNotFoundException)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "task_not_found", $"Task '{taskId}' was not found.", cancellationToken);
        }

        var record = new IdempotencyRecord<AutomationTaskRunResponse>(HttpStatusCode.Accepted, run.SubmittedAt, run);
        await _idempotencyStore.SaveAsync(scope, key, record, cancellationToken);

        return await request.CreateJsonResponseAsync(HttpStatusCode.Accepted, run, cancellationToken);
    }
}
