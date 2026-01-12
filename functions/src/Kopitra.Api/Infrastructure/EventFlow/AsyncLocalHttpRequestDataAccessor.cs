using Kopitra.Api.Common;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.Api.Infrastructure.EventFlow;

public class AsyncLocalHttpRequestDataAccessor : IHttpRequestDataAccessor
{
    private static readonly AsyncLocal<HttpRequestData?> _currentHttpRequestData = new();

    public void SetHttpRequestData(HttpRequestData httpRequestData)
    {
        _currentHttpRequestData.Value = httpRequestData;
    }

    public void ClearHttpRequestData()
    {
        _currentHttpRequestData.Value = null;
    }

    public HttpRequestData? GetHttpRequestData()
    {
        return _currentHttpRequestData.Value ?? throw new InvalidOperationException("HttpRequestData is not set.");
    }

}