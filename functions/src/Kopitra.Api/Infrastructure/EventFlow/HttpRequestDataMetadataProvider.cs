using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using Kopitra.Api.Common;
using System.IdentityModel.Tokens.Jwt;

namespace Kopitra.Api.Infrastructure.EventFlow;

/// <summary>
/// Simple IMetadataProvider implementation to extract metadata from HttpRequest
/// Will be registered in DI and used by application code to attach metadata to commands/events.
/// </summary>
public class EventFlowMetadataProvider : IMetadataProvider
{
    private readonly IHttpRequestDataAccessor _requestDataAccessor;

    public EventFlowMetadataProvider(IHttpRequestDataAccessor requestDataAccessor)
    {
        _requestDataAccessor = requestDataAccessor;
    }

    public IEnumerable<KeyValuePair<string, string>> ProvideMetadata<TAggregate, TIdentity>(TIdentity id, IAggregateEvent aggregateEvent, IMetadata metadata)
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        var requestData = _requestDataAccessor.GetHttpRequestData();
        if (requestData == null)
            return [];
        var dict = new Dictionary<string, string>();
        var headers = requestData.Headers;
        if (headers.TryGetValues("x-request-id", out var requestIdValues))
        {
            dict.Add("requestId", requestIdValues.First());
        }
        else
        {
            dict.Add("requestId", Guid.NewGuid().ToString());
        }

        if (headers.TryGetValues("x-forwarded-for", out var forwardedForValues))
        {
            dict.Add("clientIp", forwardedForValues.First());
        }
        else if (headers.TryGetValues("x-client-ip", out var clientIpValues))
        {
            dict.Add("clientIp", clientIpValues.First());
        }
        else if (headers.TryGetValues("x-real-ip", out var realIpValues))
        {
            dict.Add("clientIp", realIpValues.First());
        }

        if (headers.TryGetValues("user-agent", out var userAgentValues))
        {
            dict.Add("userAgent", userAgentValues.First());
        }

        // If authentication middleware placed user id in claims, include it
        if (headers.TryGetValues("authorization", out var authorizationValues))
        {
            var bearerToken = authorizationValues.First();
            var jwtTokenString = bearerToken.StartsWith("Bearer ") ? bearerToken.Substring("Bearer ".Length) : null;
            if (jwtTokenString != null)
            {
                var jsonWebTokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = jsonWebTokenHandler.ReadToken(jwtTokenString) as JwtSecurityToken;
                if (jsonToken != null)
                {
                    var sub = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                    var isAdmin = jsonToken.Claims.FirstOrDefault(c => c.Type == "role" && c.Value == "Admin") != null;
                    if (sub != null)
                    {
                        dict["initiatorId"] = sub;
                        dict["initiatorIsAdmin"] = isAdmin ? "true" : "false";
                    }

                }
            }
        }

        return dict;
    }
}
