using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Gateway;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class CreateCopyTradeGroupFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly CopyTradeGroupBroadcaster _broadcaster;

    public CreateCopyTradeGroupFunction(
        ICommandDispatcher commandDispatcher,
        AdminRequestContextFactory contextFactory,
        CopyTradeGroupBroadcaster broadcaster)
    {
        _commandDispatcher = commandDispatcher;
        _contextFactory = contextFactory;
        _broadcaster = broadcaster;
    }

    [Function("CreateCopyTradeGroup")]
    [OpenApiOperation(operationId: "CreateCopyTradeGroup", tags: new[] { "CopyTradeGroups" }, Summary = "Create copy trade group", Description = "Creates a new copy-trade group and publishes membership events.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateCopyTradeGroupRequest), Required = true, Description = "Copy-trade group definition.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(CopyTradeGroupReadModel), Summary = "Copy-trade group created", Description = "The created copy-trade group read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/copy-trade/groups")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<CreateCopyTradeGroupRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.GroupId) || string.IsNullOrWhiteSpace(payload.Name) || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "groupId, name, and requestedBy are required.", cancellationToken);
            }

            var command = new CreateCopyTradeGroupCommand(context.TenantId, payload.GroupId, payload.Name, payload.Description, payload.RequestedBy);
            var resultModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            await _broadcaster.BroadcastAsync(resultModel, cancellationToken).ConfigureAwait(false);
            return await request.CreateJsonResponseAsync(HttpStatusCode.Created, resultModel, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.Conflict, "operation_conflict", ex.Message, cancellationToken);
        }
    }

    private sealed record CreateCopyTradeGroupRequest(
        [property: Required] string GroupId,
        [property: Required] string Name,
        string? Description,
        [property: Required] string RequestedBy);
}
