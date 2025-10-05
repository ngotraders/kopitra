using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.OpsConsole;

public sealed class PostOpsConsoleCommandFunction
{
    private readonly AdminRequestContextFactory _contextFactory;

    public PostOpsConsoleCommandFunction(AdminRequestContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Function("PostOpsConsoleCommand")]
    [OpenApiOperation(
        operationId: "PostOpsConsoleCommand",
        tags: new[] { "OpsConsole" },
        Summary = "Post operations command",
        Description = "Enqueues an operations command issued from the console UI and echoes the pending event.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(PostOpsConsoleCommandRequest),
        Required = true,
        Description = "Operations command input, including scope and issuing operator.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(object),
        Summary = "Command event",
        Description = "The pending command event acknowledged by the management API.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Summary = "Invalid request",
        Description = "The request headers or body are invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "opsconsole/commands")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);

            var body = await request.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "invalid_body",
                    "Request body is required.",
                    cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<PostOpsConsoleCommandRequest>(
                body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (payload is null)
            {
                return await request.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "invalid_body",
                    "Request body could not be parsed.",
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(payload.Command) ||
                string.IsNullOrWhiteSpace(payload.Scope) ||
                string.IsNullOrWhiteSpace(payload.Operator))
            {
                return await request.CreateErrorResponseAsync(
                    HttpStatusCode.BadRequest,
                    "invalid_body",
                    "command, scope, and operator are required.",
                    cancellationToken);
            }

            var responsePayload = new Dictionary<string, object?>
            {
                ["id"] = $"cmd-{Guid.NewGuid():N}",
                ["command"] = payload.Command,
                ["scope"] = payload.Scope,
                ["operator"] = payload.Operator,
                ["issuedAt"] = DateTimeOffset.UtcNow,
                ["status"] = "pending",
            };

            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, responsePayload, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }

    private sealed record PostOpsConsoleCommandRequest(string Command, string Scope, string Operator);
}
