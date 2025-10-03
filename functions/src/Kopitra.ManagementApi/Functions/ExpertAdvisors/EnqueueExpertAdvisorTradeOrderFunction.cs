using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Gateway;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class EnqueueExpertAdvisorTradeOrderFunction
{
    private readonly IGatewayAdminClient _gatewayClient;
    private readonly AdminRequestContextFactory _contextFactory;

    public EnqueueExpertAdvisorTradeOrderFunction(
        IGatewayAdminClient gatewayClient,
        AdminRequestContextFactory contextFactory)
    {
        _gatewayClient = gatewayClient;
        _contextFactory = contextFactory;
    }

    [Function("EnqueueExpertAdvisorTradeOrder")]
    [OpenApiOperation(operationId: "EnqueueExpertAdvisorTradeOrder", tags: new[] { "ExpertAdvisors" }, Summary = "Enqueue expert advisor trade order", Description = "Queues a trade command for an expert advisor session via the trade gateway.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "expertAdvisorId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Expert advisor identifier", Description = "The identifier of the expert advisor.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "sessionId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Session identifier", Description = "The session identifier issued by the trade gateway.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(EnqueueExpertAdvisorTradeOrderRequest), Required = true, Description = "Trade command payload." )]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Accepted, Summary = "Trade order enqueued", Description = "The trade order has been queued for the expert advisor session.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/experts/{expertAdvisorId}/sessions/{sessionId}/trade-orders")] HttpRequestData request,
        string expertAdvisorId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<EnqueueExpertAdvisorTradeOrderRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccountId) || string.IsNullOrWhiteSpace(payload.CommandType) || string.IsNullOrWhiteSpace(payload.Instrument))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "accountId, commandType, and instrument are required.", cancellationToken);
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
                payload.Metadata);

            await _gatewayClient.EnqueueTradeOrderAsync(
                payload.AccountId,
                sessionId,
                command,
                cancellationToken).ConfigureAwait(false);

            var response = request.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }

    private sealed record EnqueueExpertAdvisorTradeOrderRequest(
        [property: Required] string AccountId,
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
        IDictionary<string, object>? Metadata);
}
