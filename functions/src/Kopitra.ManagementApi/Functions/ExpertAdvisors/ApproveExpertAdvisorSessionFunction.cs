using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Sessions;
using Kopitra.ManagementApi.Time;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class ApproveExpertAdvisorSessionFunction
{
    private readonly IServiceBusPublisher _publisher;
    private readonly IExpertAdvisorSessionDirectory _sessionDirectory;
    private readonly ServiceBusOptions _serviceBusOptions;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IClock _clock;

    public ApproveExpertAdvisorSessionFunction(
        IServiceBusPublisher publisher,
        IExpertAdvisorSessionDirectory sessionDirectory,
        IOptions<ServiceBusOptions> serviceBusOptions,
        AdminRequestContextFactory contextFactory,
        IClock clock)
    {
        _publisher = publisher;
        _sessionDirectory = sessionDirectory;
        _serviceBusOptions = serviceBusOptions.Value;
        _contextFactory = contextFactory;
        _clock = clock;
    }

    [Function("ApproveExpertAdvisorSession")]
    [OpenApiOperation(operationId: "ApproveExpertAdvisorSession", tags: new[] { "ExpertAdvisors" }, Summary = "Approve expert advisor session", Description = "Approves an expert advisor session and promotes it in the trade gateway.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "expertAdvisorId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Expert advisor identifier", Description = "The identifier of the expert advisor.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "sessionId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Session identifier", Description = "The session identifier issued by the trade gateway.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ApproveExpertAdvisorSessionRequest), Required = true, Description = "Approval metadata including authentication fingerprint.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Accepted, Summary = "Session approved", Description = "The session has been promoted in the trade gateway.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/experts/{expertAdvisorId}/sessions/{sessionId}/approve")] HttpRequestData request,
        string expertAdvisorId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<ApproveExpertAdvisorSessionRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccountId) || string.IsNullOrWhiteSpace(payload.AuthKeyFingerprint))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "accountId and authKeyFingerprint are required.", cancellationToken);
            }

            var approvedBy = payload.ApprovedBy ?? context.Principal?.Identity?.Name;

            var envelope = new
            {
                type = "authApproval",
                accountId = payload.AccountId,
                sessionId,
                authKeyFingerprint = payload.AuthKeyFingerprint,
                approvedBy,
                expiresAt = payload.ExpiresAt,
            };

            await _publisher
                .PublishAsync(_serviceBusOptions.AdminQueueName, envelope, cancellationToken)
                .ConfigureAwait(false);

            var record = new ExpertAdvisorSessionRecord(
                payload.AccountId,
                sessionId,
                payload.AuthKeyFingerprint,
                approvedBy,
                _clock.UtcNow,
                payload.ExpiresAt);

            await _sessionDirectory.RegisterAsync(record, cancellationToken).ConfigureAwait(false);

            var response = request.CreateResponse(HttpStatusCode.Accepted);
            return response;
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }

    private sealed record ApproveExpertAdvisorSessionRequest(
        [property: Required] string AccountId,
        [property: Required] string AuthKeyFingerprint,
        string? ApprovedBy,
        DateTimeOffset? ExpiresAt);
}
