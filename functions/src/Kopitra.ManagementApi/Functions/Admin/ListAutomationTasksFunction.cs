using System.Net;
using Kopitra.ManagementApi.Automation;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.Admin;

public sealed class ListAutomationTasksFunction
{
    private readonly IAutomationTaskService _taskService;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListAutomationTasksFunction(IAutomationTaskService taskService, AdminRequestContextFactory contextFactory)
    {
        _taskService = taskService;
        _contextFactory = contextFactory;
    }

    [Function("ListAutomationTasks")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/tasks")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        AdminRequestContext context;
        try
        {
            context = _contextFactory.Create(request, requireIdempotencyKey: false);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }

        var tasks = await _taskService.ListTasksAsync(context.TenantId, cancellationToken);
        var payload = new AutomationTaskListResponse(tasks.Count, tasks);
        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, payload, cancellationToken);
    }
}
