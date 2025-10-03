using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kopitra.ManagementApi.Infrastructure.Gateway;

public sealed class HttpGatewayAdminClient : IGatewayAdminClient
{
    private readonly HttpClient _client;
    private readonly ILogger<HttpGatewayAdminClient> _logger;
    private readonly Uri _enqueueUri;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public HttpGatewayAdminClient(
        HttpClient client,
        IOptions<GatewayAdminClientOptions> options,
        ILogger<HttpGatewayAdminClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var baseUrl = options.Value.BaseUrl ?? throw new InvalidOperationException("Gateway base URL is not configured.");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Gateway base URL '{baseUrl}' is not a valid absolute URI.");
        }

        _client.BaseAddress = baseUri;
        _enqueueUri = new Uri("/trade-agent/v1/admin/enqueue", UriKind.Relative);
    }

    public Task ApproveSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? approvedBy,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "authApproval",
            ["accountId"] = accountId,
            ["sessionId"] = sessionId,
            ["authKeyFingerprint"] = authKeyFingerprint,
            ["approvedBy"] = approvedBy,
            ["expiresAt"] = expiresAt,
        };

        return PostEnvelopeAsync(payload, cancellationToken);
    }

    public Task RejectSessionAsync(
        string accountId,
        Guid sessionId,
        string authKeyFingerprint,
        string? rejectedBy,
        string? reason,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "authReject",
            ["accountId"] = accountId,
            ["sessionId"] = sessionId,
            ["authKeyFingerprint"] = authKeyFingerprint,
            ["rejectedBy"] = rejectedBy,
            ["reason"] = reason,
        };

        return PostEnvelopeAsync(payload, cancellationToken);
    }

    public Task QueueOutboxEventAsync(
        string accountId,
        Guid sessionId,
        string eventType,
        object payload,
        bool requiresAck,
        CancellationToken cancellationToken)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["type"] = "queueOutboxEvent",
            ["accountId"] = accountId,
            ["sessionId"] = sessionId,
            ["eventType"] = eventType,
            ["payload"] = payload,
            ["requiresAck"] = requiresAck,
        };

        return PostEnvelopeAsync(envelope, cancellationToken);
    }

    public Task EnqueueTradeOrderAsync(
        string accountId,
        Guid sessionId,
        TradeOrderCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["type"] = "tradeOrder",
            ["accountId"] = accountId,
            ["sessionId"] = sessionId,
            ["command"] = new
            {
                command.CommandType,
                command.Instrument,
                command.OrderType,
                command.Side,
                command.Volume,
                command.Price,
                command.StopLoss,
                command.TakeProfit,
                command.TimeInForce,
                command.PositionId,
                command.ClientOrderId,
                command.Metadata,
            },
        };

        return PostEnvelopeAsync(envelope, cancellationToken);
    }

    public async Task<GatewaySessionSummary?> GetActiveSessionAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync($"/trade-agent/v1/admin/accounts/{Uri.EscapeDataString(accountId)}/sessions/active", cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<GatewaySessionSummary>(_serializerOptions, cancellationToken)
            .ConfigureAwait(false);
        return summary;
    }

    private async Task PostEnvelopeAsync(IDictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        using var response = await _client.PostAsJsonAsync(_enqueueUri, payload, _serializerOptions, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Gateway admin request failed with status {StatusCode}: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
