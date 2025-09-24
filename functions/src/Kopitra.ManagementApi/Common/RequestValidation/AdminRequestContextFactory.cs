using System.Linq;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed class AdminRequestContextFactory
{
    private const string TenantHeader = "X-TradeAgent-Account";
    private const string IdempotencyHeader = "Idempotency-Key";

    public AdminRequestContext Create(HttpRequestData request, bool requireIdempotencyKey)
    {
        return Create(request.Headers, requireIdempotencyKey);
    }

    public AdminRequestContext Create(HttpHeadersCollection headers, bool requireIdempotencyKey)
    {
        var tenant = ExtractHeaderValue(headers, TenantHeader);
        if (string.IsNullOrWhiteSpace(tenant))
        {
            throw new HttpRequestValidationException("missing_account_header", "X-TradeAgent-Account header is required.");
        }

        var idempotencyKey = ExtractHeaderValue(headers, IdempotencyHeader);
        if (requireIdempotencyKey && string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new HttpRequestValidationException("missing_idempotency_key", "Idempotency-Key header is required.");
        }

        return new AdminRequestContext(tenant, string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey);
    }

    private static string? ExtractHeaderValue(HttpHeadersCollection headers, string headerName)
    {
        if (!headers.TryGetValues(headerName, out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault();
        return value?.Trim();
    }
}
