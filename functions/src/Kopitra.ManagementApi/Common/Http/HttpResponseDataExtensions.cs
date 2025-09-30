using System.Net;
using Kopitra.ManagementApi.Common.Errors;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Common.Http;

public static class HttpResponseDataExtensions
{
    public static async Task<HttpResponseData> CreateJsonResponseAsync<T>(this HttpRequestData request, HttpStatusCode statusCode, T payload, CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken);
        return response;
    }

    public static Task<HttpResponseData> CreateErrorResponseAsync(this HttpRequestData request, HttpStatusCode statusCode, string code, string message, CancellationToken cancellationToken)
    {
        return request.CreateJsonResponseAsync(statusCode, new ErrorResponse(code, message), cancellationToken);
    }
}
