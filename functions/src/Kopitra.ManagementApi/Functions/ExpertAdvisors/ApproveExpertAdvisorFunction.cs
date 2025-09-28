using System.IO;
using System.Net;
using System.Text.Json;
using Kopitra.ManagementApi.Common.Cqrs;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Commands;
using Kopitra.ManagementApi.Application.ExpertAdvisors.Queries;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Kopitra.ManagementApi.Functions.ExpertAdvisors;

public sealed class ApproveExpertAdvisorFunction
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly AdminRequestContextFactory _contextFactory;
    private readonly IIdempotencyStore _idempotencyStore;

    public ApproveExpertAdvisorFunction(
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

    [Function("ApproveExpertAdvisor")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/experts/{expertAdvisorId}/approve")] HttpRequestData request,
        string expertAdvisorId,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = _contextFactory.Create(request, requireIdempotencyKey: true);
            var body = await new StreamReader(request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_body", "Request body is required.", cancellationToken);
            }

            var payload = JsonSerializer.Deserialize<ApproveExpertAdvisorRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (payload is null || string.IsNullOrWhiteSpace(payload.ApprovedBy))
            {
                return await request.CreateErrorResponseAsync(HttpStatusCode.BadRequest, "invalid_request", "approvedBy is required.", cancellationToken);
            }

            var hash = InMemoryIdempotencyStore.ComputeHash(body);
            var result = await _idempotencyStore.TryStoreAsync(context.TenantId, context.IdempotencyKey!, hash, cancellationToken);
            if (!result.IsNew)
            {
                var existing = await _queryDispatcher.DispatchAsync(new GetExpertAdvisorQuery(context.TenantId, expertAdvisorId), cancellationToken);
                if (existing is null)
                {
                    return await request.CreateErrorResponseAsync(HttpStatusCode.NotFound, "expert_not_found", "Expert advisor not found.", cancellationToken);
                }

                return await request.CreateJsonResponseAsync(HttpStatusCode.OK, existing, cancellationToken);
            }

            var command = new ApproveExpertAdvisorCommand(context.TenantId, expertAdvisorId, payload.ApprovedBy);
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

    private sealed record ApproveExpertAdvisorRequest(string ApprovedBy);
}
