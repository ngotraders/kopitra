# syntax=docker/dockerfile:1
FROM rust:1.76
WORKDIR /workspace
COPY tests/acceptance/Cargo.toml tests/acceptance/Cargo.toml
COPY tests/acceptance/tests tests/acceptance/tests
RUN cargo fetch --manifest-path tests/acceptance/Cargo.toml
CMD ["cargo", "test", "--manifest-path", "tests/acceptance/Cargo.toml", "--", "--test-threads=1"]
