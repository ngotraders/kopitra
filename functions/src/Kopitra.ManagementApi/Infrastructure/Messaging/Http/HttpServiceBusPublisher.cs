using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kopitra.ManagementApi.Infrastructure.Messaging.Http;

public sealed class HttpServiceBusPublisher : IServiceBusPublisher
{
    private readonly HttpClient _client;
    private readonly ILogger<HttpServiceBusPublisher> _logger;
    private readonly ServiceBusOptions _options;

    public HttpServiceBusPublisher(
        HttpClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<HttpServiceBusPublisher> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.EmulatorBaseUrl))
        {
            throw new InvalidOperationException("Service Bus emulator base URL is not configured.");
        }

        if (!Uri.TryCreate(_options.EmulatorBaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Service Bus emulator base URL '{_options.EmulatorBaseUrl}' is not a valid absolute URI.");
        }

        _client.BaseAddress = baseUri;
    }

    public async Task PublishAsync(string topicName, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(topicName))
        {
            throw new ArgumentException("Topic name must be provided.", nameof(topicName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var requestUri = new Uri($"queues/{Uri.EscapeDataString(topicName)}/messages", UriKind.Relative);

        using var response = await System.Net.Http.Json.HttpClientJsonExtensions
            .PostAsJsonAsync(_client, requestUri, new { body = payload }, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Failed to publish Service Bus message to {Topic} with status {StatusCode}: {Body}",
                topicName,
                response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }
    }

    public IReadOnlyCollection<(string Topic, object Payload)> DequeueAll() => Array.Empty<(string, object)>();
}
