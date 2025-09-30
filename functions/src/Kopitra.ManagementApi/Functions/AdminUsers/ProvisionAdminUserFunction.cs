using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.AdminUsers.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Domain.AdminUsers;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.AdminUsers;

public sealed class ProvisionAdminUserFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public ProvisionAdminUserFunction(
        ICommandDispatcher commandDispatcher,
        AdminRequestContextFactory contextFactory)
    {
        _commandDispatcher = commandDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("ProvisionAdminUser")]
    [OpenApiOperation(operationId: "ProvisionAdminUser", tags: new[] { "AdminUsers" }, Summary = "Provision admin user", Description = "Creates a management admin user and configures their roles.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ProvisionAdminUserRequest), Required = true, Description = "Admin user provisioning details, including initial roles.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(AdminUserReadModel), Summary = "Admin user provisioned", Description = "The created admin user read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/users")] HttpRequestData request,
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

            var payload = JsonSerializer.Deserialize<ProvisionAdminUserRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.UserId) || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.DisplayName) || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "userId, email, displayName, and requestedBy are required.", cancellationToken);
            }

            var roles = payload.Roles?.Select(r => Enum.TryParse<AdminUserRole>(r, true, out var role) ? role : (AdminUserRole?)null).ToList();
            if (roles is null || roles.Any(r => r is null))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_roles", "One or more roles are invalid.", cancellationToken);
            }

            var command = new ProvisionAdminUserCommand(context.TenantId, payload.UserId, payload.Email, payload.DisplayName, roles!.Select(r => r!.Value).ToArray(), payload.RequestedBy);
            var readModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.Created, readModel, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await request.CreateErrorResponseAsync(HttpStatusCode.Conflict, "operation_conflict", ex.Message, cancellationToken);
        }
    }

    private sealed record ProvisionAdminUserRequest(
        [property: Required] string UserId,
        [property: Required, EmailAddress] string Email,
        [property: Required] string DisplayName,
        [property: Required] string RequestedBy,
        IReadOnlyCollection<string>? Roles);
}
