# Gateway Contribution Guide

## Documentation
- Author all documentation and backlog entries in English.
- Record outstanding EA counterparty service follow-up items in `gateway/TODO.md`.

## Coding Practices
- Maintain deterministic behavior for session management, authentication, and event delivery.
- Extend or update automated tests when modifying handlers, state transitions, or telemetry capture.

## Required Checks
Before submitting changes under `gateway/`, run:

- `cargo fmt --manifest-path gateway/Cargo.toml`
- `cargo test --manifest-path gateway/Cargo.toml`
