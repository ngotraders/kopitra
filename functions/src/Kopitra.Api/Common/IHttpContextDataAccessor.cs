using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.Api.Common;

public interface IHttpRequestDataAccessor
{
    void SetHttpRequestData(HttpRequestData httpRequestData);
    void ClearHttpRequestData();
    HttpRequestData? GetHttpRequestData();
}