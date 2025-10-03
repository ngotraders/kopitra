FROM rust:1.74 as builder
WORKDIR /app
COPY servicebus-emulator/Cargo.toml servicebus-emulator/Cargo.lock* ./
RUN mkdir src && echo "fn main() {}" > src/main.rs
RUN cargo build --release
RUN rm -rf src
COPY servicebus-emulator/src ./src
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=builder /app/target/release/servicebus-emulator /usr/local/bin/servicebus-emulator
EXPOSE 7075
CMD ["servicebus-emulator"]
