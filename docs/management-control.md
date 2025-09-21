# Management Control Surfaces

## Overview
The management console interacts with the copy-trading platform through a dedicated management plane. Expert Advisors (EAs) must never invoke these surfaces directly; they instead use the EA Web API documented in the repository root. Administrative workflows—such as approving EA authentication keys, triggering manual orders, or orchestrating back-office tasks—either call hardened HTTPS endpoints or publish commands to Azure Service Bus queues.

## HTTPS Management API
The HTTPS endpoints exposed to administrators follow the same header rules as the public EA API (`X-TradeAgent-Account`, `Idempotency-Key`, and `X-TradeAgent-Request-ID`) while layering administrator authentication and authorization. All payloads must be JSON encoded in UTF-8.

### `POST /trade-agent/v1/sessions/{sessionId}/orders`
* **Purpose** – Queue trade instructions for an authenticated EA session. Accepted commands include market opens, incremental closes, and flatting existing positions.
* **Audience** – Management console or automation services acting on behalf of human operators. Direct EA access is forbidden.
* **Headers** – `Authorization` bearer token representing an admin principal, `X-TradeAgent-Account` for tenancy scoping, and `Idempotency-Key` to guarantee single execution.
* **Body** – `{ "instrument": "EURUSD", "side": "buy", "volume": 0.1, "timeInForce": "ioc", "metadata": { ... } }`. Validation matches the EA outbox format so rejected orders surface explicit error reasons.
* **Behavior** – When the targeted session is authenticated, the gateway emits an `OrderCommand` into the session outbox. Pending or terminated sessions receive HTTP 409 with diagnostic metadata.
* **Concurrency** – The gateway enforces a single live session per account/authentication fingerprint. Orders queued for a superseded session automatically fail with `SESSION_NOT_ACTIVE`.

### Management Azure Functions (`/api/admin/*`)
The Functions app exposes additional administrative endpoints. Typical patterns include:

* `POST /api/admin/tasks/{taskId}/run` – Start long-running maintenance jobs. Successful calls enqueue Service Bus work items for asynchronous processing.
* `GET /api/admin/accounts` – Return account inventory and entitlements for the management UI.

Both routes require operator authentication, log audit events, and emit correlation identifiers so Service Bus consumers can tie events back to their origin.

## Service Bus Messaging
Azure Service Bus provides the asynchronous contract for approvals, rejections, and downstream processing. Messages are serialized as JSON with camelCase properties. Each consumer is responsible for deleting messages once processed to avoid replay storms.

### Admin Session Approval Queue
The gateway subscribes to the queue defined by the `EA_SERVICE_BUS_QUEUE` environment variable. Messages use the following envelope:

```json
{
  "type": "authApproval",
  "accountId": "123456",
  "sessionId": "4f7f4b22-2a4d-4b9f-8f06-1afbe7192e4c",
  "authKeyFingerprint": "sha256:...",
  "approvedBy": "operator@example.com",
  "expiresAt": "2024-06-30T12:00:00Z"
}
```

* `authApproval` – Promote a pending session to authenticated. The gateway logs the approving principal and optional expiry.
* `authReject` – Deny the pending session using the same schema plus `reason` and `rejectedBy` fields.
* Messages that deserialize successfully but fail validation (for example, mismatched fingerprints) are logged and deleted to keep the queue healthy.
* Empty or malformed payloads are deleted after a warning so they do not block subsequent approvals.

### Operational Event Fan-out
Azure Functions listen to the `trade-agent-events` queue/topic. The gateway pushes EA telemetry (execution notices, health updates, acknowledgements) into this channel after the inbox/outbox handshake completes. Downstream processors must:

1. Use the provided `eventId` and `sequence` fields to guarantee idempotent handling.
2. Persist completion checkpoints before calling `Complete`/`Delete` to avoid message loss on retry.
3. Propagate `X-TradeAgent-Request-ID` from message properties into logs for traceability.

## Access Control & Operational Rules
* Maintain separate credentials for EA traffic and management operators. Management tokens grant access to the endpoints described above and must never ship inside EA binaries.
* Service Bus policies should scope send/receive rights appropriately: the management console sends approvals/rejections, while the gateway requires `Listen` permissions only.
* Document any follow-up work specific to the EA counterparty backlog in `gateway/TODO.md` and EA work in `ea/TODO.md` as directed in the repository root instructions.
* Monitor the Service Bus dead-letter queue for approval messages that repeatedly fail validation and remediate them before they accumulate.
