# EA Counterparty Gateway TODO

## Session and Authentication Enhancements
- [ ] Replace the in-memory session map with a persistent store shared across replicas.
- [ ] Enforce configurable idle session timeouts and surface metrics for heartbeat compliance.

## Event Pipeline Improvements
- [x] Support batching for outbox acknowledgements to reduce request volume during volatile markets.
- [ ] Emit structured telemetry for inbox processing, including rejection reasons and latency histograms.
- [ ] Accept and normalize RFC3339 `occurredAt` timestamps from EA inbox events so agents can supply precise timing metadata.
- [ ] Introduce replay protection for duplicate heartbeat and status events beyond basic idempotency keys.

## Azure Service Bus Integration
- [ ] Surface Service Bus listener health information on the `/trade-agent/v1/health` endpoint.
- [ ] Implement dead-letter handling for malformed or unauthorized admin commands.
- [ ] Emit metrics for Service Bus receive latency, command outcomes, and delete failures.

## Testing and Tooling
- [ ] Add load and soak tests to exercise concurrent MT4 EA connections.
- [ ] Extend negative test coverage for header validation and unauthorized access paths.
- [ ] Provide a contract test suite that validates EA expectations against the HTTP API schema.
