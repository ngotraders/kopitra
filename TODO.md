# Project Planning TODO

## EA Counterparty Service TODO
### EA / Counterparty Topology
- Operate the EA as an event-driven client inside MT4/MT5, while the counterparty service exposes a hybrid REST + WebSocket gateway for low-latency coordination.
- Guarantee **one active session per account + auth key**. When a new session is requested with the same composite key, atomically retire the previous session, notify the old EA instance, and promote the newcomer.
- Maintain per-session sequence numbers that are monotonically increasing across reconnects so event ordering is deterministic even after preemption.

### Authentication and Session Lifecycle
- Primary authentication: `AccountSessionKey` formed from the EA-provided `account_id` plus a locally generated or pre-shared authentication key. Secrets are hashed at rest and compared using constant-time checks.
- Admin console verification: the backoffice UI publishes `AuthApproval` or `AuthReject` commands to an Azure Service Bus queue/topic (`ea-auth-approvals`). The gateway subscribes to these messages to promote `pending` sessions to `authenticated` status or to terminate unapproved sessions.
- Support optional future providers (broker-issued tokens, OAuth client credentials) by modelling authentication methods as strategy plug-ins with their own validation routines.
- Session creation endpoint: `POST /ea-gateway/v1/sessions` accepts `account_id`, `auth_method`, `auth_key`, `platform`, and EA build metadata. Responses include the authoritative `session_id`, current status (`pending` or `authenticated`), and heartbeat cadence.
- Persist sessions in a store keyed by the composite `account_id` + `auth_key` fingerprint to simplify single-connection enforcement across horizontally scaled instances.
- While a session is `pending`, allow telemetry ingestion (`InitRequest`, `StatusHeartbeat`, diagnostics) but suppress outbox event delivery. Promote sessions after receiving a matching `AuthApproval` from the admin console or auto-approval policy.
- Emit structured logs whenever `pending` sessions attempt prohibited operations so operators can audit attempted order flow prior to approval.
- Configure heartbeat timers that close sessions missing `StatusHeartbeat` events beyond threshold and send a `ShutdownNotice` to prompt EA reconnection.

### Admin Console and Azure Service Bus Integration
- Provision an Azure Service Bus namespace dedicated to EA control plane messaging. Use topics to fan out approval events to all gateway replicas.
- Define message contracts:
  - `AuthApproval`: `{ accountId, authKeyFingerprint, approvedBy, expiresAt }` promoting a session and optionally setting a validity window.
  - `AuthReject`: `{ accountId, authKeyFingerprint, reason, rejectedBy }` signalling the gateway to terminate the pending session and notify the EA.
- Build a control-plane worker inside the counterparty service that listens to the queue/topic, validates message authenticity (SAS tokens / Azure AD), and updates session state accordingly.
- Record all admin-driven state changes in an audit log table (timestamp, operator, action, session snapshot) for compliance review.

### Event Catalogue (EA ⇔ Counterparty Service)
- `InitRequest` / `InitAck`: bootstrap capabilities. `InitRequest` is accepted while `pending` to collect platform metadata, but `InitAck` with operational directives is deferred until authentication succeeds.
- `StatusHeartbeat`: periodic health updates and status snapshots from the EA. Used for liveness, latency metrics, and to trigger promotion if coupled with an approved key.
- `StatusSummary`: richer status payload emitted on major state changes (e.g., spread alerts, connectivity degradation) to aid monitoring dashboards.
- `OrderIntent`: EA-provided trade opportunities queued until the session is `authenticated`. Intent metadata includes desired volume, symbol, and trade rationale for auditability.
- `OrderCommand`: execution instructions from the counterparty service to the EA (enter/exit positions, modify orders). Suppressed for `pending` sessions and replayed immediately after promotion.
- `OrderCancel`: explicit cancellation directives when risk guards or operator interventions demand it. Requires acknowledgement from the EA with correlated sequence numbers.
- `ExecutionReport`: MT4/MT5 execution outcomes mapped to broker tickets and TradeAgent sequence IDs for deterministic reconciliation and profit tracking.
- `SyncSnapshot`: bidirectional state synchronization for balances, open positions, and outstanding orders, triggered at session start and on-demand integrity checks.
- `ErrorAlert`: anomaly and policy violation notices. For `pending` sessions, capture these server-side and emit operator logs instead of forwarding to the EA.
- `ShutdownNotice`: graceful shutdown handshake issued before maintenance windows or deliberate disconnects to guarantee deterministic session closure. Includes an optional `reason` and suggested reconnect window.


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

## Ops Console Activation
- [x] Implement a management API login endpoint that issues development access tokens for admin users.
- [x] Update the ops console snapshot to hydrate the current user and admin directory from live read models.
- [x] Build a frontend login experience that captures credentials, persists the issued token, and gates protected routes.
- [x] Wire sign-out flows to clear cached tokens and return operators to the login screen.
- [x] Add backend and frontend tests covering the new authentication capabilities.
