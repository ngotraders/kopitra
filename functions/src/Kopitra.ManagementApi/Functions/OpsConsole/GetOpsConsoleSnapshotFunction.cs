using System.Collections.Generic;
using System.Net;
using Kopitra.ManagementApi.Common.Http;
using Kopitra.ManagementApi.Common.RequestValidation;
using Kopitra.ManagementApi.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Kopitra.ManagementApi.Functions.OpsConsole;

public sealed class GetOpsConsoleSnapshotFunction
{
    private readonly AdminRequestContextFactory _contextFactory;

    public GetOpsConsoleSnapshotFunction(AdminRequestContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [Function("GetOpsConsoleSnapshot")]
    [OpenApiOperation(
        operationId: "GetOpsConsoleSnapshot",
        tags: new[] { "OpsConsole" },
        Summary = "Get ops console snapshot",
        Description = "Returns the snapshot of dashboard, operations, and administration data consumed by the ops console UI.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("bearer_token", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(object),
        Summary = "Ops console snapshot",
        Description = "Aggregated console data.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Summary = "Invalid request",
        Description = "The request headers are invalid.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "opsconsole/snapshot")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _contextFactory.CreateAsync(request, cancellationToken).ConfigureAwait(false);

            var payload = new
            {
                navigationItems = new[]
                {
                    new { id = "dashboard", label = "Dashboard", to = "/dashboard/activity" },
                    new { id = "operations", label = "Operations", to = "/operations/overview" },
                    new { id = "copy-groups", label = "Copy Groups", to = "/copy-groups" },
                    new { id = "trade-agents", label = "Trade Agents", to = "/trade-agents" },
                    new { id = "admin", label = "Admin", to = "/admin/users" },
                    new { id = "integration", label = "Integration", to = "/integration/copy-trading" },
                },
                currentUser = new
                {
                    id = "user-1",
                    name = "Alex Morgan",
                    email = "alex.morgan@example.com",
                    roles = new[] { "operator" },
                },
                statMetrics = new[]
                {
                    new
                    {
                        id = "copy-rate",
                        label = "Copy Success Rate",
                        value = "98.6%",
                        delta = 2.1,
                        description = "Successful downstream fills in the last 24h.",
                    },
                    new
                    {
                        id = "latency",
                        label = "Median Latency",
                        value = "184 ms",
                        delta = -8.5,
                        description = "Median replication latency across all accounts.",
                    },
                    new
                    {
                        id = "risk-flags",
                        label = "Risk Flags",
                        value = "4",
                        delta = 1.0,
                        description = "Open guardrails requiring review.",
                    },
                    new
                    {
                        id = "sandbox-usage",
                        label = "Sandbox Usage",
                        value = "32%",
                        delta = 4.6,
                        description = "Signals routed through paper trading environments.",
                    },
                },
                activities = new[]
                {
                    new
                    {
                        id = "act-1",
                        timestamp = "2024-04-22T09:15:00Z",
                        user = "Alex Morgan",
                        action = "Promoted strategy \"Momentum\" to production",
                        status = "success",
                        target = "Strategy Desk",
                    },
                    new
                    {
                        id = "act-2",
                        timestamp = "2024-04-22T08:52:00Z",
                        user = "Jordan Mills",
                        action = "Paused replication for EU accounts",
                        status = "warning",
                        target = "Account Group EU-22",
                    },
                    new
                    {
                        id = "act-3",
                        timestamp = "2024-04-22T08:20:00Z",
                        user = "Samira Lee",
                        action = "Updated broker credentials",
                        status = "success",
                        target = "Prime Broker API",
                    },
                    new
                    {
                        id = "act-4",
                        timestamp = "2024-04-22T07:55:00Z",
                        user = "Compliance Bot",
                        action = "Flagged 3 trades breaching exposure limit",
                        status = "error",
                        target = "Risk Guardrail",
                    },
                    new
                    {
                        id = "act-5",
                        timestamp = "2024-04-22T07:12:00Z",
                        user = "Ops Automations",
                        action = "Rolled over futures contracts",
                        status = "success",
                        target = "CME Desk",
                    },
                },
                dashboardTrends = new[]
                {
                    new { id = "notifications", label = "Notifications Sent", current = "18.2K", previous = "17.1K", delta = 6.4 },
                    new { id = "fills", label = "Fills Completed", current = "16.9K", previous = "15.2K", delta = 11.2 },
                    new { id = "pnl", label = "Aggregate P&L", current = "+$482K", previous = "+$401K", delta = 20.2 },
                },
                operationsHealth = new[]
                {
                    new
                    {
                        id = "agents-online",
                        label = "Trade Agents Online",
                        value = "128",
                        status = "good",
                        helper = "All production trade agents reported heartbeats in the last 2 minutes.",
                    },
                    new
                    {
                        id = "sessions-stalled",
                        label = "Stalled Sessions",
                        value = "3",
                        status = "degraded",
                        helper = "Two sandbox sessions and one production session are awaiting broker reconnect.",
                    },
                    new
                    {
                        id = "command-queue",
                        label = "Command Queue Depth",
                        value = "7",
                        status = "good",
                        helper = "Commands are clearing within the 30 second SLA.",
                    },
                    new
                    {
                        id = "incidents-open",
                        label = "Open Incidents",
                        value = "1",
                        status = "attention",
                        helper = "Copy group APAC Momentum is under review for latency deviation.",
                    },
                },
                operationsIncidents = new[]
                {
                    new
                    {
                        id = "incident-1",
                        title = "Latency spike on BrokerPrime",
                        severity = "major",
                        openedAt = "2024-04-22T07:41:00Z",
                        acknowledgedAt = (string?)"2024-04-22T07:47:00Z",
                        owner = "Alex Morgan",
                        status = "acknowledged",
                        summary = "Median replication latency exceeded the 3s SLA for APAC Momentum accounts after broker maintenance.",
                    },
                    new
                    {
                        id = "incident-2",
                        title = "Command queue backlog",
                        severity = "minor",
                        openedAt = "2024-04-22T06:58:00Z",
                        acknowledgedAt = (string?)null,
                        owner = "Automation Engine",
                        status = "open",
                        summary = "A burst of credential rotation requests is waiting for manual approval. No replication impact observed.",
                    },
                    new
                    {
                        id = "incident-3",
                        title = "Sandbox EA heartbeat missed",
                        severity = "minor",
                        openedAt = "2024-04-22T05:12:00Z",
                        acknowledgedAt = (string?)"2024-04-22T05:30:00Z",
                        owner = "Jordan Mills",
                        status = "resolved",
                        summary = "Sandbox Alpha EA-310 stopped publishing heartbeats during overnight test run.",
                    },
                },
                commandPresets = new[]
                {
                    new
                    {
                        id = "preset-1",
                        name = "Restart sandbox agents",
                        description = "Sequential restart for sandbox accounts in preparation for release testing.",
                        targetCount = 16,
                        lastRun = "2024-04-21T21:00:00Z",
                    },
                    new
                    {
                        id = "preset-2",
                        name = "Pause EU high-risk",
                        description = "Pause replication for EU high-volatility accounts after risk guard triggers.",
                        targetCount = 12,
                        lastRun = "2024-04-22T06:50:00Z",
                    },
                    new
                    {
                        id = "preset-3",
                        name = "Rotate broker credentials",
                        description = "Cycle broker API credentials ahead of planned maintenance windows.",
                        targetCount = 24,
                        lastRun = "2024-04-22T05:30:00Z",
                    },
                },
                commandEvents = new[]
                {
                    new
                    {
                        id = "cmd-1",
                        command = "Pause replication",
                        scope = "Copy group EU-22",
                        issuedAt = "2024-04-22T08:52:00Z",
                        @operator = "Jordan Mills",
                        status = "executed",
                    },
                    new
                    {
                        id = "cmd-2",
                        command = "Restart agent",
                        scope = "Trade agent TA-1402",
                        issuedAt = "2024-04-22T08:12:00Z",
                        @operator = "Alex Morgan",
                        status = "pending",
                    },
                    new
                    {
                        id = "cmd-3",
                        command = "Promote release",
                        scope = "Trade agent TA-1127",
                        issuedAt = "2024-04-22T07:45:00Z",
                        @operator = "Samira Lee",
                        status = "failed",
                    },
                },
                operationsPerformanceTrends = new[]
                {
                    new { id = "conversion", label = "Notification → Fill Conversion", current = "92.8%", previous = "89.4%", delta = 3.4 },
                    new { id = "latency", label = "Median Fill Latency", current = "1.84s", previous = "2.10s", delta = -12.4 },
                    new { id = "pnl", label = "Net P&L (7d)", current = "+$1.26M", previous = "+$1.09M", delta = 15.6 },
                },
                copyTradeFunnelStages = new[]
                {
                    new { id = "production", label = "Production", notifications = 18200, acknowledgements = 17640, fills = 16980, pnl = 482000 },
                    new { id = "sandbox", label = "Sandbox", notifications = 2100, acknowledgements = 2056, fills = 1988, pnl = 11800 },
                },
                copyTradePerformanceAggregates = new[]
                {
                    new
                    {
                        id = "production-24h",
                        timeframe = "24h",
                        environment = "Production",
                        notifications = 18200,
                        tradeAgentsReached = 134,
                        fills = 16980,
                        pnl = 482000,
                        fillRate = 0.933,
                        avgPnlPerAgent = 3597,
                    },
                    new
                    {
                        id = "production-7d",
                        timeframe = "7d",
                        environment = "Production",
                        notifications = 118400,
                        tradeAgentsReached = 138,
                        fills = 110560,
                        pnl = 1258000,
                        fillRate = 0.934,
                        avgPnlPerAgent = 9116,
                    },
                    new
                    {
                        id = "sandbox-24h",
                        timeframe = "24h",
                        environment = "Sandbox",
                        notifications = 2100,
                        tradeAgentsReached = 22,
                        fills = 1988,
                        pnl = 11800,
                        fillRate = 0.947,
                        avgPnlPerAgent = 536,
                    },
                    new
                    {
                        id = "sandbox-7d",
                        timeframe = "7d",
                        environment = "Sandbox",
                        notifications = 12100,
                        tradeAgentsReached = 24,
                        fills = 11342,
                        pnl = 71400,
                        fillRate = 0.937,
                        avgPnlPerAgent = 2975,
                    },
                },
                copyGroupSummaries = new[]
                {
                    new
                    {
                        id = "asia-momentum",
                        name = "APAC Momentum",
                        environment = "Production",
                        status = "attention",
                        members = 42,
                        tradeAgents = 8,
                        notifications24h = 1800,
                        fills24h = 1632,
                        pnl24h = 48200,
                    },
                    new
                    {
                        id = "latam-swing",
                        name = "LATAM Swing",
                        environment = "Production",
                        status = "healthy",
                        members = 28,
                        tradeAgents = 6,
                        notifications24h = 1224,
                        fills24h = 1189,
                        pnl24h = 27100,
                    },
                    new
                    {
                        id = "sandbox-alpha",
                        name = "Sandbox Alpha",
                        environment = "Sandbox",
                        status = "paused",
                        members = 15,
                        tradeAgents = 3,
                        notifications24h = 420,
                        fills24h = 401,
                        pnl24h = 0,
                    },
                },
                copyGroupMembers = new Dictionary<string, object[]>
                {
                    ["asia-momentum"] = new object[]
                    {
                        new { id = "ea-101", name = "EA Stellar Momentum", role = "Trade Agent", status = "active", pnl7d = 18200 },
                        new { id = "ea-102", name = "EA Apex Scalper", role = "Trade Agent", status = "active", pnl7d = 14500 },
                        new { id = "trader-401", name = "Trader Chen", role = "Trader", status = "active", pnl7d = 12100 },
                    },
                    ["latam-swing"] = new object[]
                    {
                        new { id = "ea-220", name = "EA Rio Swing", role = "Trade Agent", status = "active", pnl7d = 9300 },
                        new { id = "trader-511", name = "Trader Lopez", role = "Trader", status = "inactive", pnl7d = 3100 },
                    },
                    ["sandbox-alpha"] = new object[]
                    {
                        new { id = "ea-310", name = "EA Alpha Dev", role = "Trade Agent", status = "inactive", pnl7d = -200 },
                    },
                },
                copyGroupRoutes = new Dictionary<string, object[]>
                {
                    ["asia-momentum"] = new object[]
                    {
                        new { id = "route-1", destination = "BrokerPrime / APAC-01", weight = 55, status = "healthy" },
                        new { id = "route-2", destination = "BrokerPrime / APAC-02", weight = 35, status = "degraded" },
                        new { id = "route-3", destination = "FalconFX / APAC", weight = 10, status = "healthy" },
                    },
                    ["latam-swing"] = new object[]
                    {
                        new { id = "route-4", destination = "BrokerPrime / LATAM", weight = 70, status = "healthy" },
                        new { id = "route-5", destination = "FalconFX / LATAM", weight = 30, status = "healthy" },
                    },
                    ["sandbox-alpha"] = new object[]
                    {
                        new { id = "route-6", destination = "Sandbox Broker / Test-01", weight = 100, status = "healthy" },
                    },
                },
                copyGroupPerformance = new Dictionary<string, object[]>
                {
                    ["asia-momentum"] = new object[]
                    {
                        new { agentId = "ea-101", agentName = "EA Stellar Momentum", notifications = 620, fills = 586, pnl = 21800, winRate = 68, latencyMs = 1450 },
                        new { agentId = "ea-102", agentName = "EA Apex Scalper", notifications = 540, fills = 501, pnl = 16700, winRate = 64, latencyMs = 1620 },
                    },
                    ["latam-swing"] = new object[]
                    {
                        new { agentId = "ea-220", agentName = "EA Rio Swing", notifications = 410, fills = 399, pnl = 9300, winRate = 62, latencyMs = 1810 },
                    },
                    ["sandbox-alpha"] = new object[]
                    {
                        new { agentId = "ea-310", agentName = "EA Alpha Dev", notifications = 210, fills = 203, pnl = -200, winRate = 48, latencyMs = 2120 },
                    },
                },
                tradeAgents = new[]
                {
                    new
                    {
                        id = "ta-1402",
                        name = "EA Stellar Momentum",
                        status = "online",
                        environment = "Production",
                        release = "2024.04.18",
                        activeSessions = 3,
                        copyGroupCount = 2,
                    },
                    new
                    {
                        id = "ta-1127",
                        name = "EA Apex Scalper",
                        status = "degraded",
                        environment = "Production",
                        release = "2024.04.10",
                        activeSessions = 2,
                        copyGroupCount = 1,
                    },
                    new
                    {
                        id = "ta-981",
                        name = "EA Alpha Dev",
                        status = "offline",
                        environment = "Sandbox",
                        release = "2024.03.01",
                        activeSessions = 0,
                        copyGroupCount = 1,
                    },
                },
                tradeAgentSessions = new Dictionary<string, object[]>
                {
                    ["ta-1402"] = new object[]
                    {
                        new
                        {
                            id = "session-9001",
                            brokerAccount = "BrokerPrime-APAC-01",
                            environment = "Production",
                            status = "active",
                            startedAt = "2024-04-20T22:00:00Z",
                            lastHeartbeat = "2024-04-22T09:17:00Z",
                            latencyMs = 1420,
                        },
                        new
                        {
                            id = "session-9002",
                            brokerAccount = "BrokerPrime-APAC-02",
                            environment = "Production",
                            status = "active",
                            startedAt = "2024-04-21T00:10:00Z",
                            lastHeartbeat = "2024-04-22T09:16:30Z",
                            latencyMs = 1590,
                        },
                    },
                    ["ta-1127"] = new object[]
                    {
                        new
                        {
                            id = "session-8101",
                            brokerAccount = "BrokerPrime-EU-14",
                            environment = "Production",
                            status = "pending",
                            startedAt = "2024-04-22T07:30:00Z",
                            lastHeartbeat = "2024-04-22T08:55:00Z",
                            latencyMs = 2380,
                        },
                    },
                    ["ta-981"] = new object[]
                    {
                        new
                        {
                            id = "session-7300",
                            brokerAccount = "Sandbox-Test-01",
                            environment = "Sandbox",
                            status = "closed",
                            startedAt = "2024-04-18T02:15:00Z",
                            lastHeartbeat = "2024-04-18T08:44:00Z",
                            latencyMs = 0,
                        },
                    },
                },
                tradeAgentCommands = new Dictionary<string, object[]>
                {
                    ["ta-1402"] = new object[]
                    {
                        new { id = "cmd-ta-1", issuedAt = "2024-04-22T08:12:00Z", @operator = "Alex Morgan", command = "Restart agent", status = "pending" },
                        new { id = "cmd-ta-2", issuedAt = "2024-04-21T21:42:00Z", @operator = "Ops Automation", command = "Reload config", status = "executed" },
                    },
                    ["ta-1127"] = new object[]
                    {
                        new { id = "cmd-ta-3", issuedAt = "2024-04-22T07:45:00Z", @operator = "Samira Lee", command = "Promote release", status = "failed" },
                    },
                    ["ta-981"] = new object[]
                    {
                        new { id = "cmd-ta-4", issuedAt = "2024-04-18T08:20:00Z", @operator = "Release Bot", command = "Shut down", status = "executed" },
                    },
                },
                tradeAgentLogs = new Dictionary<string, object[]>
                {
                    ["session-9001"] = new object[]
                    {
                        new { id = "log-1", timestamp = "2024-04-22T09:16:55Z", level = "info", message = "Heartbeat acknowledged (latency 1.4s)." },
                        new { id = "log-2", timestamp = "2024-04-22T09:10:12Z", level = "warn", message = "Detected slippage above threshold for GBPJPY." },
                    },
                    ["session-8101"] = new object[]
                    {
                        new { id = "log-3", timestamp = "2024-04-22T08:45:31Z", level = "error", message = "Broker authentication challenge failed (retry scheduled)." },
                    },
                    ["session-7300"] = new object[]
                    {
                        new { id = "log-4", timestamp = "2024-04-18T08:30:00Z", level = "info", message = "Sandbox session closed by operator request." },
                    },
                },
                users = new[]
                {
                    new { id = "user-1", name = "Alex Morgan", email = "alex.morgan@example.com", role = "Admin", lastActive = "2024-04-22T09:18:00Z", status = "active" },
                    new { id = "user-2", name = "Jordan Mills", email = "jordan.mills@example.com", role = "Operator", lastActive = "2024-04-22T08:59:00Z", status = "active" },
                    new { id = "user-3", name = "Samira Lee", email = "samira.lee@example.com", role = "Analyst", lastActive = "2024-04-21T23:20:00Z", status = "pending" },
                },
                userActivity = new Dictionary<string, object[]>
                {
                    ["user-1"] = new object[]
                    {
                        new { id = "ua-1", timestamp = "2024-04-22T08:52:00Z", action = "Issued \"Pause replication\" command for Copy group EU-22", ip = "10.0.12.4" },
                        new { id = "ua-2", timestamp = "2024-04-22T07:15:00Z", action = "Approved trade agent session ta-1402/session-9001", ip = "10.0.12.4" },
                    },
                    ["user-2"] = new object[]
                    {
                        new { id = "ua-3", timestamp = "2024-04-22T08:52:00Z", action = "Paused replication for Copy group EU-22", ip = "10.0.18.9" },
                    },
                    ["user-3"] = new object[]
                    {
                        new { id = "ua-4", timestamp = "2024-04-21T22:11:00Z", action = "Exported copy group performance report", ip = "10.0.18.10" },
                    },
                },
            };

            return await request.CreateJsonResponseAsync(HttpStatusCode.OK, payload, cancellationToken);
        }
        catch (HttpRequestValidationException ex)
        {
            return await request.CreateErrorResponseAsync(ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken);
        }
    }
}
