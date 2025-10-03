# syntax=docker/dockerfile:1
FROM rust:1.76 as builder
WORKDIR /app
COPY gateway/Cargo.toml gateway/Cargo.lock ./
COPY gateway/src ./src
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY --from=builder /app/target/release/gateway /usr/local/bin/gateway
ENV HOST=0.0.0.0
ENV PORT=8080
EXPOSE 8080
CMD ["gateway"]
