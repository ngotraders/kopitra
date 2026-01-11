using System.Net;
using System.Text.Json;
using Kopitra.Api.Application.Signals.Commands;
using Kopitra.Api.Functions.Signals.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Kopitra.Api.Functions.Signals;

public class CreateSignalFunction
{
    private readonly CreateSignalCommandHandler _handler = new();

    [Function("CreateSignalFunction")]
    [OpenApiOperation(operationId: "CreateSignal", tags: new[] { "Signals" }, Summary = "Create a new trading signal")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateSignalRequest), Description = "Signal creation payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(CreateSignalResponse), Description = "Signal created successfully")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid input payload")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Unexpected server error")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "signals")] HttpRequestData req,
        FunctionContext ctx)
    {
        var request = await JsonSerializer.DeserializeAsync<CreateSignalRequest>(req.Body);
        var signalId = new Kopitra.Api.Domain.ValueObjects.SignalId($"signal-{System.Guid.NewGuid()}");

        var cmd = new CreateSignalCommand(signalId)
        {
            ProviderId = new Kopitra.Api.Domain.ValueObjects.UserId(request!.ProviderId),
            Symbol = Kopitra.Api.Domain.ValueObjects.TradingSymbol.Create(request.Symbol),
            Action = Kopitra.Api.Domain.ValueObjects.OrderAction.Open,
            PositionSize = Kopitra.Api.Domain.ValueObjects.PositionSize.CreateFixedLot(request.PositionSize),
            RiskManagement = new Kopitra.Api.Domain.ValueObjects.RiskManagement()
        };

        var result = await _handler.HandleAsync(cmd);

        var res = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new CreateSignalResponse { SignalId = result.Value });
        return res;
    }
}
