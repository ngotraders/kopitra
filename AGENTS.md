# TradeAgentEA Operating Guide

These instructions govern changes within this repository. When modifying or extending functionality, ensure that TradeAgentEA continues to satisfy the requirements below.

## Documentation Language
- Author all repository documentation in English, even when implementation requests are delivered in Japanese.

## AgentMode
- TradeAgentEA operates in **synchronous relay** mode by default. Signals received must be validated and dispatched to broker endpoints within the same request lifecycle.
- A **deferred replay** mode MAY be enabled for backfill operations. Code enabling this path MUST gate the feature behind an explicit configuration flag and document the associated runbook.
- All modes MUST preserve deterministic ordering of fills and must log each dispatched order with a monotonic sequence identifier.

## Communication Headers
- Every inbound request MUST include `X-TradeAgent-Account` to scope the tenant and authorization context.
- Propagate `X-TradeAgent-Request-ID` to downstream services to preserve distributed tracing continuity.
- Include optional `X-TradeAgent-Sandbox` header to mark paper-trading flows. Do not attempt live execution when the header is present and true.
- Reject requests missing mandatory headers with HTTP 400 and a machine-readable error payload.

## Endpoints
- `POST /trade-agent/v1/signals` ingests trade intents originating from master accounts. Validate payload schema, confirm authorization, and enqueue replication tasks.
- `POST /trade-agent/v1/executions` records broker execution callbacks. Ensure idempotency by hashing provider identifiers and broker ticket numbers.
- `GET /trade-agent/v1/health` returns readiness and liveness information with dependency probes for broker APIs, message queues, and data stores.
- `GET /trade-agent/v1/runs/{run_id}` surfaces execution audit trails, including timestamps, fill states, and risk guard outcomes.

## Idempotency Rules
- All mutating endpoints MUST accept an `Idempotency-Key` header. Persist the key alongside a digest of the normalized request body for 24 hours.
- If a duplicate request is detected, return the original response payload and status code without re-invoking side effects.
- Broker callbacks SHOULD be replay-safe. When reconciling executions, guard against double-counted fills by checking both broker ticket and TradeAgentEA sequence IDs.
- Background workers reprocessing messages MUST record completion checkpoints before acknowledging queue events to avoid replays without persisted state.
