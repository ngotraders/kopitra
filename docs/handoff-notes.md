# Integration Harness Handoff Notes

This document summarizes the state of the cross-service copy-trading acceptance work as of the latest iteration.

## Completed Work
- Replaced the management-to-gateway HTTP bridge with a Service Bus emulator and new publisher logic (`functions/src/Kopitra.ManagementApi/Infrastructure/Messaging/Http/HttpServiceBusPublisher.cs`).
- Implemented an in-memory session directory for tracking approved Expert Advisor (EA) sessions (`functions/src/Kopitra.ManagementApi/Infrastructure/Sessions`).
- Added a `CopyTradeGroupBroadcaster` that publishes membership updates to Service Bus (`functions/src/Kopitra.ManagementApi/Infrastructure/CopyTradeGroupBroadcaster.cs`).
- Updated management Functions to use the Service Bus publisher and session directory, removing the former direct Gateway client.
- Introduced a Service Bus emulator crate (`servicebus-emulator`) and wired it into `compose.yaml` alongside gateway updates for the new dependency.
- Replaced the Rust and Vitest acceptance harnesses with a consolidated Playwright suite (`tests/playwright/`) that drives the ops console integration workbench through the four copy-trading scenarios.
- Documented the revised setup in `docs/integration-test-plan.md`.

## Work in Progress
- Run `npx playwright install --with-deps` on fresh CI agents so the required browsers are available before executing the suite.
- Re-run `dotnet build` if any TypeScript helper changes require updated bindings.

## Follow-Up Testing
- Execute the Playwright acceptance suite (`cd tests/playwright && npm test`) once the services and ops console are running via Docker Compose or a remote environment.

## Known Gaps
- The session directory does not yet clear terminated sessions—add integration coverage and implementation to prevent stale entries.
- The Service Bus emulator lacks retry and duplicate-detection logic; consider stress and performance tests for burst traffic.
- The new HTTP publisher could use unit tests that verify payload formatting and error propagation.

## Environment Notes
- `docker compose` now expects the `servicebus` emulator service; build it locally with `docker compose build servicebus` if images are missing.
- The emulator stores state in memory, so restarting its container clears queued admin messages—acceptable for tests but relevant for long scenarios.
- The acceptance harness depends on gateway outbox endpoints for authentication checks; keep gateway headers stable when iterating.

