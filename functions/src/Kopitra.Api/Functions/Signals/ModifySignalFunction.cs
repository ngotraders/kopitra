using System.Net;
using System.Text.Json;
using Kopitra.Api.Functions.Signals.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Kopitra.Api.Functions.Signals;

public class ModifySignalFunction
{
    [Function("ModifySignalFunction")]
    [OpenApiOperation(operationId: "ModifySignal", tags: new[] { "Signals" }, Summary = "Modify an existing trading signal")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Description = "Signal identifier")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SignalModifyRequest), Description = "Fields to update for the signal")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SignalAckResponse), Description = "Signal modification acknowledged")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid request body")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Unexpected server error")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "signals/{id}")] HttpRequestData req,
        string id,
        FunctionContext ctx)
    {
        var request = await JsonSerializer.DeserializeAsync<SignalModifyRequest>(req.Body);

        if (request == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Request body is required" });
            return bad;
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        // Minimal stub: accept modification and return 200. Real impl should call handler.
        await res.WriteAsJsonAsync(new SignalAckResponse { SignalId = id, Message = "Modification accepted" });
        return res;
    }
}
