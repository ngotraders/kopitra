# syntax=docker/dockerfile:1
FROM rust:1.76
WORKDIR /workspace
COPY gateway/Cargo.toml gateway/Cargo.lock ./gateway/
COPY gateway/src ./gateway/src
COPY gateway/tests ./gateway/tests
CMD ["cargo", "test", "--manifest-path", "gateway/Cargo.toml", "--test", "acceptance_copy_trading"]
