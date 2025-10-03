using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kopitra.ManagementApi.Infrastructure.ReadModels;
using Microsoft.Extensions.Logging;

namespace Kopitra.ManagementApi.Infrastructure.Gateway;

public sealed class CopyTradeGroupBroadcaster
{
    private readonly IGatewayAdminClient _gateway;
    private readonly ILogger<CopyTradeGroupBroadcaster> _logger;

    public CopyTradeGroupBroadcaster(IGatewayAdminClient gateway, ILogger<CopyTradeGroupBroadcaster> logger)
    {
        _gateway = gateway;
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
            var session = await _gateway.GetActiveSessionAsync(member.MemberId, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                _logger.LogDebug(
                    "Skipping copy-trade broadcast for member {MemberId} because no active session is available.",
                    member.MemberId);
                continue;
            }

            await _gateway.QueueOutboxEventAsync(
                    member.MemberId,
                    session.SessionId,
                    "CopyTradeGroupUpdated",
                    payload,
                    requiresAck: true,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
