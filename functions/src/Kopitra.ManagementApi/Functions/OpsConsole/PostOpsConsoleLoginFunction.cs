using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.AdminUsers.Commands;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.Authentication;
using Kopitra.ManagementApi.Time;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.OpsConsole;

public sealed class PostOpsConsoleLoginFunction
{
    private const string TenantHeader = "X-TradeAgent-Account";
    private const string DefaultTenantId = "console";

    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IClock _clock;

    public PostOpsConsoleLoginFunction(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, IClock clock)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _clock = clock;
    }

    [Function("PostOpsConsoleLogin")]
    [OpenApiOperation(
        operationId: "PostOpsConsoleLogin",
        tags: new[] { "OpsConsole" },
        Summary = "Authenticate console operator",
        Description = "Authenticates an operator using their admin directory record and issues a development access token.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(PostOpsConsoleLoginRequest),
        Required = true,
        Description = "Operator login credentials.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(object),
        Summary = "Login successful",
        Description = "The issued development token and resolved console user.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Summary = "Unauthorized", Description = "Credentials could not be matched to an admin user.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request body is invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "opsconsole/login")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId(request.Headers);
        var body = await request.ReadAsStringAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
        }

        PostOpsConsoleLoginRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PostOpsConsoleLoginRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body could not be parsed.", cancellationToken);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.UserId))
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "email and userId are required.", cancellationToken);
        }

        var users = await _queryDispatcher.DispatchAsync(new ListAdminUsersQuery(tenantId), cancellationToken).ConfigureAwait(false);
        var match = users.FirstOrDefault(user =>
            string.Equals(user.Email, payload.Email, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(user.UserId, payload.UserId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var provisioned = await _commandDispatcher.DispatchAsync(
                new ProvisionAdminUserCommand(
                    tenantId,
                    payload.UserId.Trim(),
                    payload.Email.Trim(),
                    FormatDisplayName(payload),
                    new[] { AdminUserRole.Operator, AdminUserRole.Supervisor },
                    "ops-console"),
                cancellationToken).ConfigureAwait(false);

            match = provisioned;
        }

        var consoleRoles = match.Roles.Select(MapConsoleRole).Where(role => !string.IsNullOrEmpty(role)).Distinct().ToArray();
        if (consoleRoles.Length == 0)
        {
            consoleRoles = new[] { "operator" };
        }

        var issuedAt = _clock.UtcNow;
        var descriptor = new DevelopmentAccessTokenDescriptor(
            tenantId,
            match.UserId,
            match.DisplayName,
            match.Email,
            consoleRoles,
            issuedAt);

        var token = DevelopmentAccessTokenCodec.CreateToken(descriptor);

        var responsePayload = new
        {
            token,
            issuedAt,
            user = new
            {
                id = match.UserId,
                name = match.DisplayName,
                email = match.Email,
                roles = consoleRoles,
            },
        };

        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, responsePayload, cancellationToken);
    }

    private static string ResolveTenantId(HttpHeadersCollection headers)
    {
        if (headers.TryGetValues(TenantHeader, out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return DefaultTenantId;
    }

    private static string MapConsoleRole(AdminUserRole role)
    {
        return role switch
        {
            AdminUserRole.Operator => "operator",
            AdminUserRole.Supervisor => "admin",
            AdminUserRole.Auditor => "analyst",
            _ => string.Empty,
        };
    }

    private static string FormatDisplayName(PostOpsConsoleLoginRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            return request.UserId.Trim();
        }

        var email = request.Email.Trim();
        var atIndex = email.IndexOf('@');
        if (atIndex > 0)
        {
            var localPart = email.Substring(0, atIndex);
            return localPart.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');
        }

        return email;
    }

    private sealed record PostOpsConsoleLoginRequest(string UserId, string Email);
}
