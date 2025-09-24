using System.Net;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.Admin;

public sealed class GetAdminHealthFunction
{
    private readonly HealthReporter _healthReporter;
    private readonly AdminRequestContextFactory _contextFactory;

    public GetAdminHealthFunction(HealthReporter healthReporter, AdminRequestContextFactory contextFactory)
    {
        _healthReporter = healthReporter;
        _contextFactory = contextFactory;
    }

    [Function("GetAdminHealth")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/health")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = _contextFactory.Create(request, requireIdempotencyKey: false);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }

        var report = await _healthReporter.CreateAsync(cancellationToken);
        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, report, cancellationToken);
    }
}
