# Kopitra Copy Trading Platform

## Overview
Kopitra enables copy trading by synchronizing positions from a master trader to follower portfolios. The development scope for this repository is limited to four deliverables that form the core runtime:

1. **Expert Advisor (EA)** that runs on the trader's terminal and emits trade signals plus telemetry.
2. **Rust Counterparty Service** hosted on Azure App Service that terminates EA requests, validates payloads, and executes copy trades.
3. **Management Server API** implemented with Azure Functions to provide authenticated configuration, monitoring endpoints, and background automation.
4. **Web Frontend** delivered via Azure Static Web Apps (or equivalent) that surfaces administrative workflows, dashboards, and operational tooling.

The objective is to build these components with a serverless-first, cost-efficient footprint while relying on managed storage so that operational overhead remains low.

## Architecture Blueprint
The reference topology pairs a minimal Azure App Service footprint with serverless compute so that continuous spend is limited to the Rust counterparty service.

```mermaid
flowchart LR
    subgraph Trader[Trader Terminal]
        EA[Expert Advisor]
    end

    subgraph AppService[Azure App Service (Linux)]
        RustSvc[Rust Counterparty Service]
    end

    subgraph Functions[Azure Functions (Consumption)]
        AdminAPI[Management API & Jobs]
    end

    subgraph StaticWeb[Azure Static Web Apps]
        WebFE[Web Frontend]
    end

    subgraph Managed[Managed Azure Services]
        DB[(Azure SQL Database - Serverless)]
        Bus[(Azure Service Bus Queue/Topic)]
        Storage[(Azure Storage - Blob/Table)]
        Monitor[(Azure Monitor & Application Insights)]
    end

    EA -->|Orders & Telemetry| RustSvc
    RustSvc -->|Copy Trade Execution| Brokers[(Broker APIs)]
    RustSvc -->|Config & State| DB
    RustSvc -->|Event Fan-out| Bus
    AdminAPI -->|Operations Events| Bus
    WebFE -->|HTTPS| AdminAPI
    AdminAPI --> DB
    AdminAPI --> Storage
    Bus -->|Queue Triggers| AdminAPI
    AdminAPI -.metrics.-> Monitor
    RustSvc -.metrics.-> Monitor
```

### Component Responsibilities
- **Expert Advisor** – Publishes trade intentions, receives execution callbacks, and handles retry logic with idempotency keys.
- **Rust Counterparty Service** – Runs on Azure App Service, terminates EA traffic, executes copy trades within the service boundary, normalizes broker payloads, manages the EA-facing Web API session model, and enforces the required request headers.
- **Management Server API** – Azure Functions exposing HTTP, timer, and Service Bus triggered functions for configuration, audit reporting, and operational automation.
- **Web Frontend** – Deployed to Azure Static Web Apps, provides dashboards to observe synchronization health, manage accounts, and trigger administrative actions through the Functions API.

Copy-trade replication completes inside the Rust counterparty service to keep latency predictable; Service Bus queues carry only secondary events such as audit trail enrichment, alerting, and configuration tasks. Direct EA integration occurs exclusively over the documented Web API so that both directions of messaging stay under HTTP control with deterministic acknowledgement semantics.

## Cross-System API Specifications
This section reflects the current message flows between the four core systems. Payloads are expressed as JSON; all timestamps are ISO-8601 with millisecond precision and UTC offset (`2024-03-27T02:09:11.124Z`).

### EA ↔ Counterparty Web API Contract
The Expert Advisor connects to the Rust counterparty service through a narrow HTTP surface that enforces deterministic sequencing and mandatory headers. The canonical base path is `https://<counterparty-host>/trade-agent/v1`.

#### Required Headers
| Header | Description | Applies To |
|--------|-------------|------------|
| `X-TradeAgent-Account` | Identifies the EA account and is used for tenant scoping. Requests lacking the header are rejected with HTTP 400. | All EA-facing endpoints |
| `Idempotency-Key` | Globally unique string for deduplicating retries within 24 hours. | All mutating endpoints (`POST`, `DELETE`) |
| `X-TradeAgent-Request-ID` | Client-generated correlation ID echoed in logs and downstream calls. | Optional but recommended for every call |

Session affinity is maintained on the service side using the account header and the active lease; EA calls do not carry a bearer token or additional authorization header.

#### Session Lifecycle
| Method & Path | Purpose |
|---------------|---------|
| `POST /sessions` | Authenticate the EA and obtain an exclusive session lease. Reject with HTTP 409 when a lease is already active. |
| `DELETE /sessions/current` | Release the active lease. Safe to retry thanks to `Idempotency-Key`. |

**Sample `POST /sessions` request**

```json
{ "secret": "<shared-secret>", "clientVersion": "1.4.0" }
```

**Sample `POST /sessions` response**

```json
{ "sessionId": "sess_123", "status": "pending", "pending": true }
```

**Sample `DELETE /sessions/current` response**

```json
{ "status": "released" }
```

#### EA Inbox (EA → Counterparty)
| Method & Path | Purpose | Response |
|---------------|---------|----------|
| `POST /sessions/current/inbox` | Submit EA telemetry, acknowledgements, and execution notices. | `202 Accepted` with `{ "accepted": 2 }`. Duplicate payloads return the first response. |

**Sample inbox payload**

```json
{
  "events": [
    {
      "eventType": "ea.autotrade_status",
      "eventId": "evt_1001",
      "status": "online",
      "reportedAt": "2024-03-27T02:09:11.124Z"
    },
    {
      "eventType": "OutboxAck",
      "eventId": "evt_1002",
      "acknowledgedEventId": "trade_2001"
    }
  ]
}
```

#### Counterparty Outbox (Counterparty → EA)
| Method & Path | Purpose | Response |
|---------------|---------|----------|
| `GET /sessions/current/outbox?cursor=<sequence>` | Poll for pending events after the provided cursor. When empty, returns `retryAfter` hints for exponential back-off. | Returns the next batch of ordered events or a keep-alive with `retryAfterMs`. |

**Sample outbox response**

```json
{
  "cursor": 451,
  "retryAfterMs": 2000,
  "events": [
    {
      "eventId": "trade_2001",
      "sequence": 450,
      "eventType": "trade.started",
      "payload": {
        "symbol": "USDJPY",
        "side": "buy",
        "volume": 1.2,
        "masterOrderId": "mo_8899"
      }
    }
  ]
}
```

#### Trade Execution Interfaces
| Method & Path | Purpose | Response |
|---------------|---------|----------|
| `POST /signals` | Primary entry point for trade intents from the EA. Enforces header validation and logs with a monotonic sequence ID. | `202 Accepted` with `{ "status": "queued", "sequence": 982 }`. |
| `POST /executions` | Broker callback endpoint. Payload must include broker ticket identifiers and fill quantities for idempotent reconciliation. | `200 OK` with `{ "status": "recorded" }`. Duplicate notifications return the original body with HTTP 200. |
| `GET /health` | Standard readiness probe. Includes dependency summaries for SQL, Service Bus, and broker APIs. | JSON body summarizing downstream health. |

**Sample trade signal payload**

```json
{
  "signalId": "sig_4471",
  "masterOrderId": "mo_8899",
  "symbol": "USDJPY",
  "side": "buy",
  "volume": 1.2,
  "timeInForce": "GTC"
}
```

**Sample execution callback payload**

```json
{
  "broker": "Oanda",
  "ticket": "12345678",
  "masterOrderId": "mo_8899",
  "fillQuantity": 1.2,
  "fillPrice": 132.114,
  "executedAt": "2024-03-27T02:15:44.991Z"
}
```

**Sample health response**

```json
{ "status": "healthy", "dependencies": { "sql": "ok", "serviceBus": "ok", "brokers": "degraded" }}
```

### Counterparty ↔ Management Service Bus Contracts
The Rust counterparty service and the Azure Functions management API communicate asynchronously through Azure Service Bus. Both sides use peek-lock semantics with a 30-second lock duration and dead-letter messages that exceed three delivery attempts.

#### Queues and Topics
| Name | Direction | Entity Type | Purpose |
|------|-----------|-------------|---------|
| tradeagent.operations | Counterparty → Management API | Queue | Carries operational events, audit records, and retryable background work for Functions triggers. |
| tradeagent.commands | Management API → Counterparty | Queue | Delivers administrative commands such as resync requests and configuration pushes back to the counterparty service. |

#### Shared Application Properties
| Property | Description |
|----------|-------------|
| messageId | Globally unique identifier generated by the publishing service. Reused to ensure idempotent handling on the consumer side. |
| correlationId | Mirrors X-TradeAgent-Request-ID when present so HTTP and queue traces can be joined. |
| subject | Short descriptor of the message type (for example execution.audit). |
| contentType | Always application/json. |
| scheduledEnqueueTimeUtc | Optional delay used by the management API when scheduling deferred maintenance tasks. |

#### `tradeagent.operations` Message Schema
The counterparty service emits the following envelope to describe operational events that require management processing.

```json
{
  "messageType": "execution.audit",
  "account": "acct_2049",
  "sequence": 982,
  "occurredAt": "2024-03-27T02:15:44.991Z",
  "payload": {
    "masterOrderId": "mo_8899",
    "status": "filled",
    "fillQuantity": 1.2,
    "fillPrice": 132.114
  }
}
```

- `messageType` controls the Function trigger routing (for example, `execution.audit`, `ea.telemetry`, `alert.trigger`).
- `sequence` aligns with the counterparty service monotonic sequence to preserve ordering guarantees.
- `payload` content varies by message type but must remain under 64 KB to avoid exceeding the Service Bus maximum message size for the Basic tier.

#### `tradeagent.commands` Message Schema
The management API publishes commands to the counterparty service for deferred administration. The counterparty polls using a background worker that respects the peek-lock timeout and explicitly completes processed messages.

```json
{
  "commandType": "ea.resync",
  "account": "acct_2049",
  "issuedAt": "2024-03-27T03:02:11.552Z",
  "parameters": {
    "fromSequence": 870,
    "reason": "operator-request"
  }
}
```

- `commandType` identifies the handler (for example, `ea.resync`, `session.forceRelease`, `config.publish`).
- `parameters` is an object whose structure depends on the command type. Empty objects are allowed for commands that require no additional arguments.
- Consumers MUST settle the message (complete or dead-letter) before the peek-lock expires to prevent duplicate execution.

## Managed Platform Strategy
All persistent or stateful resources use managed Azure services so that infrastructure remains low-touch and pay-as-you-go.

| Use Case | Service | Notes |
|----------|---------|-------|
| Relational data (accounts, trade history, configuration) | **Azure SQL Database (Serverless, Hyperscale optional)** | Auto-pause at 1 hour idle keeps spend low while still offering burst capacity for busy sessions. |
| Event fan-out, automation triggers | **Azure Service Bus (Basic tier queue or topic)** | Provides ordered delivery for EA-generated events that Azure Functions consume for operational tasks. |
| File and report artifacts | **Azure Blob Storage** | Stores EA logs, generated reports, and downloadable audit bundles with lifecycle policies for cold storage. |
| Secrets and app configuration | **Azure Key Vault & App Configuration** | Centralized secret rotation and feature flags; Functions and App Service use managed identity for access. |
| Telemetry and diagnostics | **Azure Monitor with Application Insights** | Consolidates logs, metrics, and distributed traces without managing ingestion pipelines. |

## Cost-Optimized Deployment Plan
The default posture minimizes always-on compute by leaning on Azure Functions and serverless databases. Azure App Service hosts the latency-sensitive Rust workload while remaining within a modest monthly budget.

### Baseline Cloud Footprint (Production)
- **Azure App Service (Linux, B1)** – Runs the Rust counterparty service with reserved capacity (~USD 13/month) and supports custom domains plus managed identity.
- **Azure Functions (Consumption)** – Handles the management API and background jobs; cost scales with execution count and typically stays in the free grant for low traffic.
- **Azure SQL Database (Serverless, S0/S1)** – Configure auto-pause at 1 hour and max vCores aligned to expected concurrency (~USD 10–30/month depending on activity).
- **Azure Service Bus (Basic)** – Queues EA events for asynchronous processing (~USD 1/month) while guaranteeing ordered delivery.
- **Azure Static Web Apps (Free/Standard)** – Hosts the management frontend with integrated authentication and global CDN (~USD 0–9/month depending on tier).
- **Shared Services** – Application Insights, Blob Storage, and Key Vault add minimal base charges and scale with actual usage.

With this mix, monthly spend typically remains under USD 50 when workloads are sporadic; scale-up paths (Premium Functions plan, App Service scaling, larger SQL tiers) are deferred until copy-trade volume justifies the cost.

### Shared Development / Staging
- Reuse the production-grade Azure SQL serverless instance with short auto-pause windows or deploy a smaller S0 database per environment.
- Host the Rust service on a lower-tier App Service plan (B1) shared across dev and staging slots; use deployment slots for blue/green validation.
- Deploy Azure Functions to the same resource group using staged slots, and enable Service Bus queues with lower message TTL to manage costs.
- Cache-busting builds of the Static Web App run through GitHub Actions on each merge, providing parity with production without idle charges.

### Local & Developer Environments
- Run the Rust counterparty service via `cargo run` or Docker locally while authenticating against Azure SQL using a developer firewall rule.
- Use the Azure Functions Core Tools to emulate HTTP and Service Bus triggers, falling back to Azurite for offline queue/blob development.
- Point the EA to a local tunnel (e.g., `ngrok`, Azure Dev Tunnels) that forwards into the developer instance of the Rust service for end-to-end tests.

## API Surface & Data Flows
The Rust counterparty service handles synchronous trade execution, while Azure Functions expose management surfaces and process asynchronous events from Service Bus.

### EA-Facing HTTPS Endpoints (Rust Counterparty Service)
| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/trade-agent/v1/sessions` | Establish an authenticated EA session using the account-scoped key; first session wins while additional attempts receive HTTP 409. |
| `DELETE` | `/trade-agent/v1/sessions/current` | Release the active EA session lease; requires the same headers and idempotency key used during creation. |
| `GET` | `/trade-agent/v1/sessions/current/outbox` | Poll for pending counterparty events (trade started/partial/full close) with ordered sequence IDs. |
| `POST` | `/trade-agent/v1/sessions/current/inbox` | Push EA-originated events such as acknowledgements (`OutboxAck`), available currency pairs, and automation state updates. |
| `POST` | `/trade-agent/v1/signals` | Receive EA trade intents; validate headers and trigger immediate copy trades before queuing follow-up events. |
| `POST` | `/trade-agent/v1/executions` | Record broker execution callbacks with idempotent handling and emit reconciliation messages. |
| `GET` | `/trade-agent/v1/health` | Publish readiness checks covering downstream dependencies. |

### Management Control Interfaces
The management surface is intentionally separated from EA traffic. Requests must include an `Authorization` bearer token issued by Azure AD and a `X-TradeAgent-Request-ID` header for traceability. Additional endpoints and contracts are documented in [`docs/management-control.md`](docs/management-control.md); the summary below captures the latest cross-system touchpoints.

#### Counterparty Management Plane (Rust Service)
| Method & Path | Purpose | Response |
|---------------|---------|----------|
| `POST /trade-agent/v1/sessions/{sessionId}/orders` | Inject orders into an active EA session. Used by automated remediation jobs triggered from the management API. | `202 Accepted` with `{ "status": "queued" }`. |
| `GET /trade-agent/v1/sessions/{sessionId}/outbox` | Observability endpoint for operators to review pending events before they reach the EA. | Returns the same schema as the EA-facing outbox plus operator metadata. |

**Sample management order command**

```json
{
  "commandId": "cmd_771",
  "expiresAt": "2024-03-27T02:25:00Z",
  "payload": {
    "type": "close_position",
    "symbol": "USDJPY",
    "volume": 1.2
  }
}
```

#### Management Server API (Azure Functions)
| Method & Path | Purpose | Notes |
|---------------|---------|-------|
| `POST /api/admin/tasks/{taskId}/run` | Kick off orchestrations such as full follower resync, position reconciliation, or EA firmware rollout. The task definition determines which Service Bus messages are emitted. | Returns `{ "taskRunId": "tr_991" }` and streams progress to Application Insights. |
| `GET /api/admin/accounts` | Enumerate master/follower accounts with linked broker credentials and entitlements. Supports `?includeBrokers=true`. | Results are paginated (`nextPageToken`). |
| `GET /api/admin/orders/{orderId}` | Retrieve consolidated order state by hydrating data from SQL and recent Service Bus events. | HTTP 404 indicates the identifier is unknown or outside the caller's tenant scope. |
| `GET /api/admin/runs/{runId}/events` | Stream audit trail events captured during automated runs. | Server-sent events (SSE) stream; clients must handle keep-alive comments. |

#### Service Bus Contracts
| Queue/Topic | Publisher | Consumer | Payload Highlights |
|-------------|-----------|----------|--------------------|
| tradeagent.operations | Rust counterparty service | Azure Functions (TradeAgentEventsProcessor) | Operational events such as execution.audit, ea.telemetry, and alert.trigger. |
| tradeagent.commands | Azure Functions orchestration | Rust counterparty service background worker | Administrative commands including ea.resync, session.forceRelease, and config.publish. |
| alerts-deadletter | Any | Ops tooling | Holds poison messages. Retried events must be stamped with new X-TradeAgent-Request-ID values. |

SignalR bindings on Azure Functions supplement the HTTP API for live dashboard updates without maintaining custom socket servers.

## Development Workflow
1. **Clone the Repository**
   ```bash
   git clone git@github.com:kopitra/platform.git
   cd platform
   ```
2. **Bootstrap Environment Variables**
   - Copy `.env.sample` files for the Rust service, management API, and frontend. Populate broker credentials, managed storage connection strings, and EA authentication secrets.
   - Store secrets in Azure Key Vault and access them locally via `az keyvault secret show` to avoid plaintext storage.
3. **Install Tooling**
   - Rust toolchain (`rustup`), Azure Functions Core Tools, .NET 8 SDK (for Functions), Node.js 18+, Docker (optional), Azure CLI, and Terraform/Bicep for infrastructure changes.
4. **Run Services Locally**
   ```bash
   # Rust counterparty service
   cd gateway
   cargo run --bin trade-agent

   # Management API (Azure Functions)
   cd ../functions
   func start --csharp

   # Web frontend
   cd ../opsconsole
   npm install
   npm run dev

   # Launch the EA within its trading terminal and point it to http://localhost:8080 (or tunnel to Functions/App Service endpoints)
   ```
5. **Execute Tests & Linters**
   - Rust gateway:
     ```bash
     cd gateway
     cargo fmt --all
     cargo clippy --all-targets --all-features -- -D warnings
     cargo build --all-targets --locked
     cargo test --all --locked
     ```
   - Azure Functions management API:
     ```bash
     cd functions
     dotnet restore Functions.sln
     dotnet format Functions.sln --verify-no-changes --verbosity minimal
     dotnet build Functions.sln --no-restore
     dotnet test Functions.sln --no-build
     ```
   - Web frontend:
     ```bash
     cd opsconsole
     npm install
     npm run lint
     npm test
     npm run build
     ```
6. **Deploy Infrastructure (Optional)**
   ```bash
   az login
   az account set --subscription <subscription-id>
   az deployment sub create \
     --location japaneast \
     --template-file infra/main.bicep \
     --parameters env=dev appServicePlanSku=B1 sqlAutoPauseDelay=60
   ```

## Monitoring & Operations
- **Logging** – Emit structured JSON logs to Azure Monitor; configure Log Analytics workspaces with cost caps and retention policies aligned to compliance SLAs.
- **Metrics** – Track signal ingestion latency, EA uptime, and order reconciliation success. Set thresholds for proactive paging.
- **Runbooks** – Document EA failover, App Service slot recovery, Azure Functions queue draining, and managed storage restoration procedures. Store runbooks alongside infrastructure-as-code for version control.

## Roadmap Highlights
- Harden EA connectivity with circuit breakers and offline buffering.
- Automate cost anomaly detection using Azure Cost Management alerts.
- Expand broker integrations incrementally, gating each behind feature flags in the management API.

## References
- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure Functions Documentation](https://learn.microsoft.com/azure/azure-functions/)
- [Azure SQL Database Serverless](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-overview)
- [Azure Service Bus Documentation](https://learn.microsoft.com/azure/service-bus-messaging/)
- [Azure Static Web Apps Documentation](https://learn.microsoft.com/azure/static-web-apps/)
