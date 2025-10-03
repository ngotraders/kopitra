# Integration Test Configuration for TradeAgentEA Workflows

This document outlines how to orchestrate end-to-end integration tests that exercise the enterprise workflows described in the scenarios below. The goal is to validate business correctness by coordinating Expert Advisor (EA) instances, the management site, and downstream trading infrastructure.

## Automated Acceptance Harness

The repository now ships with executable acceptance tests that orchestrate the gateway service, management Azure Functions, and the ops console integration layer through the four business scenarios. A lightweight Azure Service Bus emulator container is included so that every management-to-EA instruction transits the same queue interface used in production, while the suites exercise the public HTTP surfaces to validate resulting EA outbox state.

- **Rust test suite** – `cargo test --manifest-path tests/acceptance/Cargo.toml` drives the gateway via its public APIs and the management Azure Functions endpoints.
- **Ops console integration suite** – `npm run test:integration` from `opsconsole/` runs Vitest-based acceptance tests that use the console’s HTTP clients to approve sessions, manage copy-trade groups, and dispatch copy orders against live gateway and Functions instances.
- **Docker Compose** – `docker compose run acceptance` executes the Rust suite, while `docker compose run opsconsole-tests` installs the console dependencies and runs the Vitest scenarios against the same containerised gateway/management stack. The compose file provisions the Service Bus emulator automatically so admin approvals, copy-trade broadcasts, and follower trade orders all flow across the queue integration without requiring Azure connectivity. Environment variables preconfigure development authentication so the tests can approve sessions and queue copy trades end-to-end.
- **CI hooks** – Invoke the commands above inside your automation. Together the suites assert session approvals, group synchronisation broadcasts, copy-trade fan-out, and trade acknowledgements across the combined services from both the API and console perspectives.

The sections below remain as the high-level manual test choreography and can be used to extend the automated harness with additional checks or cross-system assertions.

## Shared Test Foundations

The four scenarios reuse a common staging environment and automation toolchain.

- **Environment topology**
  - One staging deployment of the gateway, trade relay, and ops console aligned to a dedicated tenant ID.
  - Sandbox broker endpoints or a deterministic broker simulator to avoid live trading risk.
  - Message queue topics for signal replication and execution callbacks isolated per test run (e.g., prefixed with `itest-<timestamp>`).
- **Reference data**
  - Seed the management database with:
    - Test instruments (e.g., EURUSD, USDJPY) configured for sandbox trading.
    - Two EA accounts (`EA-A`, `EA-B`) and two operator accounts with approval rights.
    - Three trade groups (`Group-Alpha`, `Group-Beta`, `Group-Gamma`) to support multi-group routing.
- **Automation harness**
  - Use an integration test driver that can invoke management site APIs, emulate EA websocket connections, and assert broker simulator state.
  - Capture `X-TradeAgent-Request-ID` for each step and correlate with downstream logs.
  - Wrap each HTTP mutation with an `Idempotency-Key` to conform to platform rules.
- **Observability**
  - Enable verbose logging for signal intake, approval workflows, replication dispatch, and execution callbacks.
  - Persist event timelines so that assertions can validate monotonic sequence numbers and group routing.

Each scenario begins from a clean slate by resetting queues, clearing pending orders, and disconnecting active EA sessions.

## Scenario 1: Single EA Order and Settlement

**Objective**: Validate that one EA can connect, receive an order instruction from the management site, and complete the settlement flow.

1. **EA connection**
   - Launch the EA simulator as `EA-A` and establish a websocket session with the gateway.
   - Verify handshake headers (`X-TradeAgent-Account`, `X-TradeAgent-Request-ID`).
2. **Order instruction**
   - Through the management API, submit a market order for `Group-Alpha` targeting `EA-A`.
   - Assert that the approval workflow emits an approval task and that the operator approves it via the management site endpoint.
3. **Signal relay**
   - Confirm the EA receives the approved order signal with a new sequence ID.
   - Validate that the EA acknowledges receipt and the gateway dispatches the order to the broker simulator.
4. **Settlement**
   - Use the broker simulator to send an execution callback.
   - Assert the platform records the execution in `POST /trade-agent/v1/executions` and that the EA marks the order as complete.

## Scenario 2: Dual EA Copy Trading within One Group

**Objective**: Ensure two EAs can both join a group, receive approval, and copy trades initiated through the management site.

1. **Connect two EAs**
   - Establish connections for `EA-A` and `EA-B`.
   - Confirm both sessions register under the same tenant and group context.
2. **Group formation**
   - Via the management site, create `Group-Beta` if not existing and add both EAs.
   - Trigger the approval workflow; ensure operators approve both membership requests.
3. **Copy trade initiation**
   - Submit a master trade for `Group-Beta` from the management API.
   - Validate that both EAs receive identical signals with synchronized sequence numbers.
4. **Execution validation**
   - Confirm both EAs send broker orders and that the simulator receives two distinct tickets tied to the same master trade ID.
   - Ensure execution callbacks reconcile properly for each EA without cross-contamination.

## Scenario 3: Independent Orders Across Multiple Groups

**Objective**: Verify that coexisting groups can process distinct copy trades without interference.

1. **Prepare groups**
   - Reset state and reconnect `EA-A` to `Group-Alpha`, `EA-B` to `Group-Beta`, and optionally spin up `EA-C` for `Group-Gamma` if needed.
   - Confirm membership approvals per group.
2. **Dispatch parallel orders**
   - Issue separate management site orders for each group with unique `X-TradeAgent-Request-ID` values.
   - Ensure each order carries a unique idempotency key to prevent accidental replay.
3. **Assertion checks**
   - Monitor signal queues to confirm isolation: no EA should receive an instruction for a group it does not belong to.
   - Validate the broker simulator records discrete trades per group and that audit logs tag the correct group identifier.

## Scenario 4: Multi-Group EA Executing Separate Orders

**Objective**: Confirm that an EA belonging to multiple groups can correctly execute distinct orders for each group without mixing state.

1. **Membership setup**
   - Assign `EA-A` to both `Group-Alpha` and `Group-Gamma`, ensuring approvals for dual membership.
   - Maintain `EA-B` exclusively within `Group-Beta` for control comparisons.
2. **Order issuance**
   - Create two orders nearly simultaneously: one targeting `Group-Alpha`, another for `Group-Gamma`.
   - Use unique idempotency keys and request IDs.
3. **Signal routing validation**
   - Verify `EA-A` receives two separate signals, each tagged with its respective group metadata.
   - Confirm sequencing is monotonic within each group and that acknowledgments reference the correct order IDs.
4. **Execution reconciliation**
   - Observe the broker simulator to ensure `EA-A` opens and settles both positions distinctly, while `EA-B` remains unaffected.
   - Check that audit logs link executions back to the appropriate group-level run IDs.

## Reporting and Exit Criteria

- Capture a consolidated report with timestamps, request IDs, sequence numbers, and final execution states.
- Tests pass when all assertions hold, no unexpected signals appear, and settlement records reconcile with broker callbacks.
- File any discrepancies as defects with reproducer details and associated request IDs.
