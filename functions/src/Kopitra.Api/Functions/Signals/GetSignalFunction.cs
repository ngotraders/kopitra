using System.Net;
using System.Text.Json;
using Kopitra.Api.Functions.Signals.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Kopitra.Api.Functions.Signals;

public class GetSignalFunction
{
    [Function("GetSignalFunction")]
    [OpenApiOperation(operationId: "GetSignal", tags: new[] { "Signals" }, Summary = "Get signal detail")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Description = "Signal identifier")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SignalDto), Description = "Signal detail payload")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Signal not found")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "signals/{id}")] HttpRequestData req,
        string id,
        FunctionContext ctx)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        // Minimal stub: return hydrated DTO with placeholder data. Replace with read model query.
        var response = new SignalDto
        {
            SignalId = id,
            ProviderId = "provider-demo",
            Symbol = "UNKNOWN",
            Action = 0,
            PositionSize = new SignalPositionSizeDto { FixedLot = null, Percentage = null, IsProportional = false },
            RiskManagement = null,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            ClosedAt = null,
            DistributedToCount = 0,
            ExecutedCount = 0
        };
        await res.WriteAsJsonAsync(response);
        return res;
    }
}
