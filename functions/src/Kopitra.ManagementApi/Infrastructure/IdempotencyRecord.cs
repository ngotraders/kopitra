using System.Net;

namespace Kopitra.ManagementApi.Infrastructure;

public sealed record IdempotencyRecord<TResponse>(HttpStatusCode StatusCode, DateTimeOffset CreatedAt, TResponse Response);
