using Kopitra.ManagementApi.Common.RequestValidation;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace Kopitra.ManagementApi.Tests.Common.RequestValidation;

public class AdminRequestContextFactoryTests
{
    private readonly AdminRequestContextFactory _factory = new();

    [Fact]
    public void Create_WithValidHeaders_ReturnsContext()
    {
        var headers = new HttpHeadersCollection
        {
            { "X-TradeAgent-Account", "demo" },
            { "Idempotency-Key", "abc-123" }
        };

        var context = _factory.Create(headers, requireIdempotencyKey: true);

        Assert.Equal("demo", context.TenantId);
        Assert.Equal("abc-123", context.IdempotencyKey);
    }

    [Fact]
    public void Create_MissingAccountHeader_Throws()
    {
        var headers = new HttpHeadersCollection();

        var exception = Assert.Throws<HttpRequestValidationException>(() => _factory.Create(headers, requireIdempotencyKey: false));

        Assert.Equal("missing_account_header", exception.ErrorCode);
    }

    [Fact]
    public void Create_WhenIdempotencyRequiredButMissing_Throws()
    {
        var headers = new HttpHeadersCollection
        {
            { "X-TradeAgent-Account", "demo" }
        };

        var exception = Assert.Throws<HttpRequestValidationException>(() => _factory.Create(headers, requireIdempotencyKey: true));

        Assert.Equal("missing_idempotency_key", exception.ErrorCode);
    }
}
