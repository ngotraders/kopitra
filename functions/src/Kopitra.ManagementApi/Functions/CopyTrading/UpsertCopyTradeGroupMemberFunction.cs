using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.CopyTrading.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class UpsertCopyTradeGroupMemberFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public UpsertCopyTradeGroupMemberFunction(
        ICommandDispatcher commandDispatcher,
        AdminRequestContextFactory contextFactory)
    {
        _commandDispatcher = commandDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("UpsertCopyTradeGroupMember")]
    [OpenApiOperation(operationId: "UpsertCopyTradeGroupMember", tags: new[] { "CopyTradeGroups" }, Summary = "Upsert copy-trade group member", Description = "Adds or updates a member configuration within a copy-trade group.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "groupId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Copy-trade group identifier", Description = "The identifier of the copy-trade group to update.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "memberId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Member identifier", Description = "The identifier of the member to upsert.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpsertCopyTradeGroupMemberRequest), Required = true, Description = "Member configuration to apply to the copy-trade group.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CopyTradeGroupReadModel), Summary = "Member upserted", Description = "The updated copy-trade group read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "admin/copy-trade/groups/{groupId}/members/{memberId}")] HttpRequestData request,
        string groupId,
        string memberId,
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

            var payload = JsonSerializer.Deserialize<UpsertCopyTradeGroupMemberRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "role, riskStrategy, allocation, and requestedBy are required.", cancellationToken);
            }

            if (!Enum.TryParse<CopyTradeMemberRole>(payload.Role, true, out var role))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_role", $"Unsupported role '{payload.Role}'.", cancellationToken);
            }

            if (!Enum.TryParse<RiskStrategy>(payload.RiskStrategy, true, out var riskStrategy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_risk", $"Unsupported risk strategy '{payload.RiskStrategy}'.", cancellationToken);
            }

            if (payload.Allocation <= 0)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_allocation", "Allocation must be greater than zero.", cancellationToken);
            }

            var command = new UpsertCopyTradeGroupMemberCommand(context.TenantId, groupId, memberId, role, riskStrategy, payload.Allocation, payload.RequestedBy);
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

    private sealed record UpsertCopyTradeGroupMemberRequest(
        [property: Required] string Role,
        [property: Required] string RiskStrategy,
        [property: Range(0.0001, double.MaxValue)] decimal Allocation,
        [property: Required] string RequestedBy);
}
