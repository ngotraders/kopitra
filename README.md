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

## EA ↔ Counterparty Web API Contract
The EA communicates with the Rust counterparty service through a strict Web API protocol. Sessions are authenticated per EA account, only one EA connection is allowed at a time, and every message requires an acknowledgement so that pending events are removed once delivered.

### Authentication & Session Control
- **Account-scoped authentication** – The EA authenticates by issuing `POST /trade-agent/v1/sessions` with the `X-TradeAgent-Account` header and a shared secret payload. Successful calls return a short-lived session token that the EA presents on subsequent requests (typically through `Authorization: Bearer <token>`). Requests that omit the header are rejected with HTTP 400.
- **First-come session leasing** – Only one active session per account is permitted. When a valid session already exists, additional `POST /trade-agent/v1/sessions` attempts receive HTTP 409 (Conflict) with metadata that instructs the EA to retry after the configured TTL or prompt the user to terminate the other session. The first authenticated EA retains the lease until it explicitly calls `DELETE /trade-agent/v1/sessions/current` or the lease expires.
- **Idempotent handshakes** – Session creation calls must include an `Idempotency-Key` header so that network retries return the original token without spawning duplicate sessions. Session revocation (`DELETE /trade-agent/v1/sessions/current`) also honors the header to avoid premature release if the EA retries.

### Counterparty → EA Event Outbox
- **Polling interface** – The EA retrieves counterparty events (e.g., `trade.started`, `trade.partial_close`, `trade.closed`) via `GET /trade-agent/v1/sessions/current/outbox?cursor=<sequence>`. Responses include an ordered list of pending events with monotonic sequence IDs to satisfy the deterministic ordering rule in `AGENTS.md`.
- **Acknowledgement workflow** – After processing events, the EA adds `OutboxAck` entries to its next inbox submission (`POST /trade-agent/v1/sessions/current/inbox`). The counterparty service deletes the acknowledged events before the following poll so successfully processed messages never reappear. Missing acknowledgements cause the event to remain pending for replay on the next poll.
- **Real-time hints** – The counterparty service MAY include a `retryAfter` field in outbox responses to advise the EA on back-off when no events are available. This keeps polling costs minimal without introducing additional transports.

### EA → Counterparty Event Inbox
- **EA telemetry endpoint** – The EA reports its automation status, available currency pairs, heartbeat state, and broker-side updates by calling `POST /trade-agent/v1/sessions/current/inbox`. Each payload is tagged with an `eventType` such as `ea.symbol_catalog`, `ea.autotrade_status`, or `ea.execution_notice`.
- **Message durability** – Inbox submissions require both `X-TradeAgent-Account` and `Idempotency-Key` headers. The counterparty service persists the payload until downstream consumers (e.g., management workflows or audit storage) mark completion. Duplicate submissions return the original response to prevent double processing.
- **Acknowledgement semantics** – Delivery confirmations are encoded as `OutboxAck` events inside the inbox payload. Each acknowledgement includes the `eventId` (and optional `sequence`) of the outbox message being retired, allowing the counterparty service to remove it without an additional HTTP round-trip.

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
Management consoles must use the dedicated management plane described in [`docs/management-control.md`](docs/management-control.md).

| Transport | Address | Hosting Component | Description |
|-----------|---------|-------------------|-------------|
| HTTPS | `POST /trade-agent/v1/sessions/{sessionId}/orders` | Rust Counterparty Service (management plane) | Instruct a specific EA session to open or close positions by queuing `OrderCommand` events. Authenticated EA traffic must not invoke this endpoint. |
| HTTPS | `POST /api/admin/tasks/{taskId}/run` | Management Server API (HTTP-triggered Function) | Trigger operational automations (e.g., resync follower) that may enqueue Service Bus jobs. |
| HTTPS | `GET /api/admin/accounts` | Management Server API (HTTP-triggered Function) | List managed accounts, entitlements, and linked brokers from Azure SQL. |
| Service Bus | `trade-agent-events` | Management Server API (Service Bus-triggered Function) | Processes EA counterparty events (audit, notifications, anomaly detection) asynchronously. |

SignalR bindings on Azure Functions can supplement the HTTP API for live dashboard updates without maintaining custom socket servers.

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
