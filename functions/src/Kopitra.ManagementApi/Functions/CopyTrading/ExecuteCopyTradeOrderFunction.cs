using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Application.CopyTrading.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.CopyTrading;
using Kopitra.ManagementApi.Infrastructure.Gateway;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.CopyTrading;

public sealed class ExecuteCopyTradeOrderFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly IGatewayAdminClient _gatewayClient;
    private readonly AdminRequestContextFactory _contextFactory;

    public ExecuteCopyTradeOrderFunction(
        IQueryDispatcher queryDispatcher,
        IGatewayAdminClient gatewayClient,
        AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _gatewayClient = gatewayClient;
        _contextFactory = contextFactory;
    }

    [Function("ExecuteCopyTradeOrder")]
    [OpenApiOperation(operationId: "ExecuteCopyTradeOrder", tags: new[] { "CopyTradeGroups" }, Summary = "Execute copy-trade order", Description = "Enqueues trade orders for all follower members of a copy-trade group.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "groupId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Copy-trade group identifier", Description = "The copy-trade group to execute against.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ExecuteCopyTradeOrderRequest), Required = true, Description = "Trade command and leader metadata.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Accepted, Summary = "Copy trade enqueued", Description = "Trade orders were enqueued for follower members.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/copy-trade/groups/{groupId}/orders")] HttpRequestData request,
        string groupId,
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

            var payload = JsonSerializer.Deserialize<ExecuteCopyTradeOrderRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.SourceAccount) || string.IsNullOrWhiteSpace(payload.CommandType) || string.IsNullOrWhiteSpace(payload.Instrument))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "sourceAccount, commandType, and instrument are required.", cancellationToken);
            }

            var query = new GetCopyTradeGroupQuery(context.TenantId, groupId);
            var group = await _queryDispatcher.DispatchAsync(query, cancellationToken).ConfigureAwait(false);
            if (group is null)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "group_not_found", "Copy trade group not found.", cancellationToken);
            }

            var leader = group.Members.FirstOrDefault(member => member.Role == CopyTradeMemberRole.Leader && string.Equals(member.MemberId, payload.SourceAccount, StringComparison.OrdinalIgnoreCase));
            if (leader is null)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "leader_not_found", "The provided source account is not registered as a leader in the group.", cancellationToken);
            }

            var followers = group.Members.Where(member => member.Role == CopyTradeMemberRole.Follower).ToArray();
            if (followers.Length == 0)
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "no_followers", "The copy-trade group has no follower members to execute.", cancellationToken);
            }

            var command = new TradeOrderCommand(
                payload.CommandType,
                payload.Instrument,
                payload.OrderType,
                payload.Side,
                payload.Volume,
                payload.Price,
                payload.StopLoss,
                payload.TakeProfit,
                payload.TimeInForce,
                payload.PositionId,
                payload.ClientOrderId,
                BuildMetadata(group, payload, leader.MemberId));

            foreach (var follower in followers)
            {
                var session = await _gatewayClient.GetActiveSessionAsync(follower.MemberId, cancellationToken).ConfigureAwait(false);
                if (session is null)
                {
                    continue;
                }

                await _gatewayClient.EnqueueTradeOrderAsync(
                    follower.MemberId,
                    session.SessionId,
                    command,
                    cancellationToken).ConfigureAwait(false);
            }

            var response = request.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }

    private static IDictionary<string, object> BuildMetadata(
        CopyTradeGroupReadModel group,
        ExecuteCopyTradeOrderRequest request,
        string leaderAccount)
    {
        var metadata = request.Metadata is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(request.Metadata);

        metadata["groupId"] = group.GroupId;
        metadata["groupName"] = group.Name;
        metadata["sourceAccount"] = leaderAccount;
        metadata["initiatedBy"] = request.InitiatedBy ?? leaderAccount;
        metadata["initiatedAt"] = DateTimeOffset.UtcNow;
        return metadata;
    }

    private sealed record ExecuteCopyTradeOrderRequest(
        [property: Required] string SourceAccount,
        [property: Required] string CommandType,
        [property: Required] string Instrument,
        string? OrderType,
        string? Side,
        double? Volume,
        double? Price,
        double? StopLoss,
        double? TakeProfit,
        string? TimeInForce,
        string? PositionId,
        string? ClientOrderId,
        string? InitiatedBy,
        IDictionary<string, object>? Metadata);
}
