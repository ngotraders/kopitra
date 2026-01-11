using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using Microsoft.AspNetCore.Http;

namespace Kopitra.Api.Infrastructure.EventFlow;

/// <summary>
/// Simple IMetadataProvider implementation to extract metadata from HttpRequest
/// Will be registered in DI and used by application code to attach metadata to commands/events.
/// </summary>
public class HttpRequestMetadataProvider : IMetadataProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestMetadataProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IEnumerable<KeyValuePair<string, string>> ProvideMetadata<TAggregate, TIdentity>(TIdentity id, IAggregateEvent aggregateEvent, IMetadata metadata)
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
        {
            return new Metadata(new Dictionary<string, string>());
        }

        var headers = ctx.Request.Headers;
        var dict = new Dictionary<string, string>
        {
            ["requestId"] = headers.TryGetValue("X-Request-Id", out var reqId) ? reqId.ToString() : Guid.NewGuid().ToString(),
            ["clientIp"] = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            ["userAgent"] = headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : string.Empty,
        };

        // If authentication middleware placed user id in claims, include it
        var user = ctx.User?.Identity;
        if (user != null && user.IsAuthenticated)
        {
            var name = ctx.User?.FindFirst("sub")?.Value ?? ctx.User?.Identity?.Name ?? string.Empty;
            dict["initiatorId"] = name;
            dict["initiatorIsAdmin"] = ctx.User?.IsInRole("Admin").ToString() ?? "false";
        }

        return dict;
    }
}
