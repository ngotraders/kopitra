using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using EventFlow.Commands;
using EventFlow.Queries;
using EventFlow;
using Kopitra.Api.Domain.ExpertAdvisors;
using Kopitra.Api.Application.ExpertAdvisors.Commands;
using Kopitra.Api.Application.ExpertAdvisors.Queries;
using Kopitra.Api.Functions.ExpertAdvisors.Models;
using System.Net;
using System.ComponentModel.DataAnnotations;

namespace Kopitra.Api.Functions.ExpertAdvisors;

public class ExpertAdvisorFunctions(ICommandBus commandBus, IQueryProcessor queryProcessor, ILogger<ExpertAdvisorFunctions> logger)
{
    [Function("CreateSession")]
    [OpenApiOperation(operationId: "CreateSession", tags: new[] { "Sessions" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateSessionRequest))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(SessionDetailResponse))]
    public async Task<HttpResponseData> CreateSession(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ea/sessions")] HttpRequestData req,
        FunctionContext context,
        CancellationToken token)
    {
        logger.LogInformation("CreateSession function triggered");

        try
        {
            var body = await req.ReadFromJsonAsync<CreateSessionRequest>(token).ConfigureAwait(false);

            if (body == null || string.IsNullOrWhiteSpace(body.UserId) || string.IsNullOrWhiteSpace(body.AccountId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "UserId and AccountId are required" }, token).ConfigureAwait(false);
                return badResponse;
            }

            var sessionId = ExpertAdvisorSessionId.New;
            var command = new CreateSessionCommand(sessionId) { UserId = body.UserId, AccountId = body.AccountId };

            await commandBus.PublishAsync(command, token).ConfigureAwait(false);

            var readModel = await queryProcessor.ProcessAsync(new GetSessionByIdQuery(sessionId.Value), token).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new SessionDetailResponse
            {
                Id = readModel.Id,
                UserId = readModel.UserId,
                AccountId = readModel.AccountId,
                State = readModel.State,
                JwtToken = readModel.JwtToken,
                CreatedAt = readModel.CreatedAt,
                LastHeartbeatAt = readModel.LastHeartbeatAt,
                ExpiresAt = readModel.ExpiresAt
            }, token).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating session");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return errorResponse;
        }
    }

    [Function("GetSessions")]
    [OpenApiOperation(operationId: "GetSessions", tags: new[] { "Sessions" })]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<SessionSummaryResponse>))]
    public async Task<HttpResponseData> GetSessions(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ea/sessions")] HttpRequestData req,
        FunctionContext context,
        CancellationToken token)
    {
        logger.LogInformation("GetSessions function triggered");

        try
        {
            var readModels = await queryProcessor.ProcessAsync(new GetAllSessionsQuery(), token).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            var sessionResponses = readModels.Select(rm => new SessionSummaryResponse
            {
                Id = rm.Id,
                UserId = rm.UserId,
                AccountId = rm.AccountId,
                State = rm.State,
                CreatedAt = rm.CreatedAt,
                LastHeartbeatAt = rm.LastHeartbeatAt,
                ExpiresAt = rm.ExpiresAt
            });

            await response.WriteAsJsonAsync(sessionResponses, token).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching sessions");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return errorResponse;
        }
    }

    [Function("GetSessionDetail")]
    [OpenApiOperation(operationId: "GetSessionDetail", tags: new[] { "Sessions" })]
    [OpenApiParameter(name: "sessionId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetailResponse))]
    public async Task<HttpResponseData> GetSessionDetail(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ea/sessions/{sessionId}")] HttpRequestData req,
        string sessionId,
        FunctionContext context,
        CancellationToken token)
    {
        logger.LogInformation($"GetSessionDetail function triggered for session {sessionId}");

        try
        {
            var readModel = await queryProcessor.ProcessAsync(new GetSessionByIdQuery(sessionId), token).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new SessionDetailResponse
            {
                Id = readModel.Id,
                UserId = readModel.UserId,
                AccountId = readModel.AccountId,
                State = readModel.State,
                JwtToken = readModel.JwtToken,
                CreatedAt = readModel.CreatedAt,
                LastHeartbeatAt = readModel.LastHeartbeatAt,
                ExpiresAt = readModel.ExpiresAt
            }, token).ConfigureAwait(false);
            return response;
        }
        catch (InvalidOperationException)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { error = $"Session {sessionId} not found" }, token).ConfigureAwait(false);
            return notFoundResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching session detail");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return errorResponse;
        }
    }

    [Function("AuthenticateSession")]
    [OpenApiOperation(operationId: "AuthenticateSession", tags: new[] { "Sessions" })]
    [OpenApiParameter(name: "sessionId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetailResponse))]
    public async Task<HttpResponseData> AuthenticateSession(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ea/sessions/{sessionId}/authenticate")] HttpRequestData req,
        string sessionId,
        FunctionContext context,
        CancellationToken token)
    {
        logger.LogInformation($"AuthenticateSession function triggered for session {sessionId}");

        try
        {
            var command = new AuthenticateSessionCommand(new ExpertAdvisorSessionId(sessionId));
            await commandBus.PublishAsync(command, token).ConfigureAwait(false);

            var readModel = await queryProcessor.ProcessAsync(new GetSessionByIdQuery(sessionId), token).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new SessionDetailResponse
            {
                Id = readModel.Id,
                UserId = readModel.UserId,
                AccountId = readModel.AccountId,
                State = readModel.State,
                JwtToken = readModel.JwtToken,
                CreatedAt = readModel.CreatedAt,
                LastHeartbeatAt = readModel.LastHeartbeatAt,
                ExpiresAt = readModel.ExpiresAt
            }, token).ConfigureAwait(false);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return badResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error authenticating session");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return errorResponse;
        }
    }

    [Function("Heartbeat")]
    [OpenApiOperation(operationId: "Heartbeat", tags: new[] { "Sessions" })]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(HeartbeatRequest))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object))]
    public async Task<HttpResponseData> Heartbeat(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ea/heartbeat")] HttpRequestData req,
        FunctionContext context,
        CancellationToken token)
    {
        logger.LogInformation("Heartbeat function triggered");

        try
        {
            var body = await req.ReadFromJsonAsync<HeartbeatRequest>(token).ConfigureAwait(false);

            if (body == null || string.IsNullOrWhiteSpace(body.SessionId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "SessionId is required" }, token).ConfigureAwait(false);
                return badResponse;
            }

            var command = new RecordHeartbeatCommand(new ExpertAdvisorSessionId(body.SessionId));
            await commandBus.PublishAsync(command, token).ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Heartbeat recorded" }, token).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording heartbeat");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message }, token).ConfigureAwait(false);
            return errorResponse;
        }
    }
}
