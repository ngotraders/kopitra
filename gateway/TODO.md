# EA Counterparty Gateway TODO

## Session and Authentication Enhancements
- [ ] Replace the in-memory session map with a persistent store shared across replicas.
- [ ] Enforce configurable idle session timeouts and surface metrics for heartbeat compliance.

## Event Pipeline Improvements
- [ ] Support batching for outbox acknowledgements to reduce request volume during volatile markets.
- [ ] Emit structured telemetry for inbox processing, including rejection reasons and latency histograms.
- [ ] Accept and normalize RFC3339 `occurredAt` timestamps from EA inbox events so agents can supply precise timing metadata.
- [ ] Introduce replay protection for duplicate heartbeat and status events beyond basic idempotency keys.

## Testing and Tooling
- [ ] Add load and soak tests to exercise concurrent MT4 EA connections.
- [ ] Extend negative test coverage for header validation and unauthorized access paths.
- [ ] Provide a contract test suite that validates EA expectations against the HTTP API schema.
