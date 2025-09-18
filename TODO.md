# Project Planning TODO

## EA Counterparty Service TODO
- **EA / counterparty service topology**
  - Operate the EA as an event-driven client inside MT4, while the counterparty service exposes a hybrid REST + WebSocket gateway for low-latency coordination.
  - Support multi-tenant broker accounts but enforce a single active EA session per account by preemptively cancelling any existing session when a new one is authorized.
- **Authentication and session management**
  - Offer pluggable authentication strategies: (a) the default `AccountSessionKey` flow for MT4 using `account_id` plus a first-launch generated or pre-shared authentication key, and (b) optional token-based or OAuth client credential adapters for future integrations.
  - Create sessions through `POST /ea-gateway/v1/sessions` supplying `account_id`, `auth_method`, and the authentication material; hash secrets at rest in the session store.
  - Persist sessions in a store keyed by the account unique identifier and session ID. When the submitted authentication key has not been verified yet, mark the session as `pending` (unauthenticated) while still capturing telemetry and logging inbound data.
  - Deny order/event emission while a session is `pending`; once the authentication key is approved (manual backoffice or automated verification), transition the session to `authenticated`, unlock event exchange, and notify the EA via a state-change message.
  - Issue heartbeat timers that close sessions with missing `StatusHeartbeat` events beyond the configured threshold and signal the EA to reconnect.
- **Event catalogue (EA ⇔ counterparty service)**
  - `InitRequest` / `InitAck`: initialize capabilities. `InitRequest` is accepted in `pending` sessions to gather environment metadata, but `InitAck` with actionable directives is deferred until authentication succeeds.
  - `StatusHeartbeat`: periodic health updates from the EA. Used both for liveness checks and to promote `pending` sessions once the authentication key has been validated.
  - `OrderIntent`: EA-provided trade opportunities, queued but not dispatched until the session becomes `authenticated`.
  - `OrderCommand`: execution instructions from the counterparty service to the EA. These are suppressed for `pending` sessions and emitted immediately after promotion, including any backlog accumulated during verification.
  - `ExecutionReport`: MT4 execution outcomes mapped to broker tickets and TradeAgent sequence IDs for deterministic reconciliation.
  - `SyncSnapshot`: bidirectional state synchronization for positions, balances, and order books, triggered at session start and on-demand integrity checks.
  - `ErrorAlert`: anomaly and policy violation notices, including remediation steps and retry policies; unauthenticated sessions only log these events server-side without dispatching to the EA.
  - `ShutdownNotice`: graceful shutdown handshake issued before maintenance windows or deliberate disconnects to guarantee deterministic session closure.


## Phased Milestones
1. **Phase 1 – Core Copy Trading Pipeline**
   - Stabilize ingestion of master account trade signals with latency budgets under 150 ms.
   - Implement secure credential vaulting and role-based access for broker connections.
   - Deliver a minimal monitoring dashboard covering trade replication health.
2. **Phase 2 – Portfolio Intelligence**
   - Introduce allocation strategies (mirrored lots, proportional risk, and fixed fractional).
   - Layer in configurable risk guards for drawdown, exposure, and trade frequency.
   - Expose performance analytics APIs for downstream reporting tools.
3. **Phase 3 – Ecosystem Integrations**
   - Add broker-specific adapters for priority partners and sandbox providers.
   - Offer webhook and message bus connectors for external automation workflows.
   - Launch developer SDKs and documentation for third-party strategy authors.

## Reliability Tasks
- Instrument trade replication pipeline with distributed tracing and SLO dashboards.
- Build automated failover for strategy hosts with warm standby replicas.
- Implement circuit breakers and dead-letter queues around broker RPC failures.
- Schedule regular disaster-recovery game days to validate runbooks.

## Technical Debt
- Refactor execution engine to isolate broker adapters behind a unified interface.
- Replace ad hoc polling loops with event-driven workers backed by message queues.
- Upgrade configuration management to support per-tenant overrides and secrets.
- Consolidate duplicated trade model validations across services.

## Documentation Backlog
- Author a "Getting Started" guide for copy-trading operators and tenant admins.
- Document broker onboarding procedures, including compliance prerequisites.
- Create reference diagrams for signal flow, scaling topology, and observability stack.
- Maintain an API changelog covering release notes and migration guidance.
