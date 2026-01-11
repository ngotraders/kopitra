using System.Net;
using System.Text.Json;
using Kopitra.Api.Functions.Signals.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Kopitra.Api.Functions.Signals;

public class CloseSignalFunction
{
    [Function("CloseSignalFunction")]
    [OpenApiOperation(operationId: "CloseSignal", tags: new[] { "Signals" }, Summary = "Close an existing signal")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Description = "Signal identifier")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SignalAckResponse), Description = "Signal close request acknowledged")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Unexpected server error")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "signals/{id}/close")] HttpRequestData req,
        string id,
        FunctionContext ctx)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        // Minimal stub: acknowledge close request
        await res.WriteAsJsonAsync(new SignalAckResponse { SignalId = id, Message = "Close request accepted" });
        return res;
    }
}
