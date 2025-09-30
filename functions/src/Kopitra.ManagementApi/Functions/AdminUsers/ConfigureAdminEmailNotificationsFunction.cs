using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Application.Notifications.Commands;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.AdminUsers;

public sealed class ConfigureAdminEmailNotificationsFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public ConfigureAdminEmailNotificationsFunction(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        AdminRequestContextFactory contextFactory,
        IIdempotencyStore idempotencyStore)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
        _idempotencyStore = idempotencyStore;
    }

    [Function("ConfigureAdminEmailNotifications")]
    [OpenApiOperation(operationId: "ConfigureAdminEmailNotifications", tags: new[] { "AdminUsers" }, Summary = "Configure admin email notifications", Description = "Updates the email notification preferences for an admin user.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Admin user identifier", Description = "The identifier of the admin user to update.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(name: "Idempotency-Key", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Idempotency key", Description = "Optional key to guarantee exactly-once processing for retried requests.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ConfigureAdminEmailNotificationsRequest), Required = true, Description = "Email notification preferences and audit metadata.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AdminUserReadModel), Summary = "Notification settings updated", Description = "The updated admin user read model.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers or body are invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Summary = "Operation conflict", Description = "A conflicting operation prevented the command from completing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "admin/users/{userId}/notifications/email")] HttpRequestData request,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<ConfigureAdminEmailNotificationsRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.RequestedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "requestedBy is required.", cancellationToken);
            }

            var topics = payload.Topics?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray() ?? Array.Empty<string>();

            var hash = InMemoryIdempotencyStore.ComputeHash(body);
            var dedupeKey = context.IdempotencyKey ?? $"{request.FunctionContext.FunctionDefinition.Name}:{hash}";
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, dedupeKey, hash, cancellationToken);
            if (!result.IsNew)
            {
                var users = await _queryDispatcher.DispatchAsync(new ListAdminUsersQuery(context.TenantId), cancellationToken);
                var user = users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.Ordinal));
                if (user is null)
                {
                    return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "user_not_found", "Admin user not found.", cancellationToken);
                }

                return await request.CreateJsonResponseAsync(HttpStatusCode.OK, user, cancellationToken);
            }

            var command = new ConfigureAdminEmailNotificationsCommand(context.TenantId, userId, payload.EmailEnabled, topics, payload.RequestedBy);
            var readModel = await _commandDispatcher.DispatchAsync(command, cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, readModel, cancellationToken);
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

    private sealed record ConfigureAdminEmailNotificationsRequest(
        bool EmailEnabled,
        IReadOnlyCollection<string>? Topics,
        [property: Required] string RequestedBy);
}
