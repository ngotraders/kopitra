using System.Net;

namespace Kopitra.ManagementApi.Common.RequestValidation;

public sealed class HttpRequestValidationException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string ErrorCode { get; }

    public HttpRequestValidationException(string errorCode, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
