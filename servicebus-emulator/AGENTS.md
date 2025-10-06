# Service Bus Emulator Contribution Guide

These instructions apply to the entire `servicebus-emulator` directory.

## Documentation
- Document behavioral changes in English.

## Required Checks
Before submitting changes under `servicebus-emulator/`, run:

- `cargo fmt --manifest-path servicebus-emulator/Cargo.toml -- --check`
- `cargo clippy --manifest-path servicebus-emulator/Cargo.toml --all-targets --all-features`
- `cargo check --manifest-path servicebus-emulator/Cargo.toml`
- `cargo test --manifest-path servicebus-emulator/Cargo.toml`
