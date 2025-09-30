using System.Linq;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed class AdminRequestContextFactory
{
    private const string TenantHeader = "X-TradeAgent-Account";
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string DefaultTenantId = "console";

    public AdminRequestContext Create(HttpRequestData request)
    {
        return Create(request.Headers);
    }

    public AdminRequestContext Create(HttpHeadersCollection headers)
    {
        var tenant = ExtractHeaderValue(headers, TenantHeader);
        if (string.IsNullOrWhiteSpace(tenant))
        {
            tenant = DefaultTenantId;
        }

        var idempotencyKey = ExtractHeaderValue(headers, IdempotencyHeader);

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
