using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Infrastructure.Messaging;
using Kopitra.ManagementApi.Infrastructure.Sessions;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kopitra.ManagementApi.Infrastructure;

public sealed class CopyTradeGroupBroadcaster
{
    private readonly IServiceBusPublisher _publisher;
    private readonly IExpertAdvisorSessionDirectory _sessionDirectory;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<CopyTradeGroupBroadcaster> _logger;

    public CopyTradeGroupBroadcaster(
        IServiceBusPublisher publisher,
        IExpertAdvisorSessionDirectory sessionDirectory,
        IOptions<ServiceBusOptions> options,
        ILogger<CopyTradeGroupBroadcaster> logger)
    {
        _publisher = publisher;
        _sessionDirectory = sessionDirectory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task BroadcastAsync(
        CopyTradeGroupReadModel group,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["groupId"] = group.GroupId,
            ["name"] = group.Name,
            ["description"] = group.Description,
            ["createdBy"] = group.CreatedBy,
            ["createdAt"] = group.CreatedAt,
            ["members"] = group.Members
                .Select(member => new Dictionary<string, object?>
                {
                    ["memberId"] = member.MemberId,
                    ["role"] = member.Role.ToString().ToLowerInvariant(),
                    ["riskStrategy"] = member.RiskStrategy.ToString(),
                    ["allocation"] = member.Allocation,
                    ["updatedAt"] = member.UpdatedAt,
                    ["updatedBy"] = member.UpdatedBy,
                })
                .ToArray(),
        };

        foreach (var member in group.Members)
        {
            var session = await _sessionDirectory.GetAsync(member.MemberId, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                _logger.LogDebug(
                    "Skipping copy-trade broadcast for member {MemberId} because no active session is registered.",
                    member.MemberId);
                continue;
            }

            var envelope = new Dictionary<string, object?>
            {
                ["type"] = "queueOutboxEvent",
                ["accountId"] = member.MemberId,
                ["sessionId"] = session.SessionId,
                ["eventType"] = "CopyTradeGroupUpdated",
                ["payload"] = payload,
                ["requiresAck"] = true,
            };

            await _publisher.PublishAsync(_options.AdminQueueName, envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}
