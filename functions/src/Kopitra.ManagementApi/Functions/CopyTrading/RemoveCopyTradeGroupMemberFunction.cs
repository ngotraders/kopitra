using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class RemoveCopyTradeGroupMemberFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public RemoveCopyTradeGroupMemberFunction(
        ICommandDispatcher commandDispatcher,
        AdminRequestContextFactory contextFactory)
    {
        _commandDispatcher = commandDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("RemoveCopyTradeGroupMember")]
    [OpenApiOperation(operationId: "RemoveCopyTradeGroupMember", tags: new[] { "CopyTradeGroups" }, Summary = "Remove copy-trade group member", Description = "Removes a member from a copy-trade group and broadcasts membership changes.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "groupId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Copy-trade group identifier", Description = "The identifier of the copy-trade group to update.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "memberId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Member identifier", Description = "The identifier of the member to remove.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RemoveCopyTradeGroupMemberRequest), Required = false, Description = "Optional metadata describing who initiated the removal.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CopyTradeGroupReadModel), Summary = "Member removed", Description = "The updated copy-trade group read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "admin/copy-trade/groups/{groupId}/members/{memberId}")] HttpRequestData request,
        string groupId,
        string memberId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            var payload = string.IsNullOrWhiteSpace(body) ? new RemoveCopyTradeGroupMemberRequest(null) : JsonSerializer.Deserialize<RemoveCopyTradeGroupMemberRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var requestedBy = payload?.RequestedBy ?? "system";

            var command = new RemoveCopyTradeGroupMemberCommand(context.TenantId, groupId, memberId, requestedBy);
            var resultModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, resultModel, cancellationToken);
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

    private sealed record RemoveCopyTradeGroupMemberRequest(string? RequestedBy);
}
