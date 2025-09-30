using System.Collections.Generic;
using System.Net;
using Kopitra.ManagementApi.Application.AdminUsers.Queries;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.AdminUsers;

public sealed class ListAdminUsersFunction
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;

    public ListAdminUsersFunction(IQueryDispatcher queryDispatcher, AdminRequestContextFactory contextFactory)
    {
        _queryDispatcher = queryDispatcher;
        _contextFactory = contextFactory;
    }

    [Function("ListAdminUsers")]
    [OpenApiOperation(operationId: "ListAdminUsers", tags: new[] { "AdminUsers" }, Summary = "List admin users", Description = "Retrieves the administrative users that can access the management API.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<AdminUserReadModel>), Summary = "Admin users", Description = "The admin users configured for the tenant.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request headers are invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/users")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            var result = await _queryDispatcher.DispatchAsync(new ListAdminUsersQuery(context.TenantId), cancellationToken);
            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
