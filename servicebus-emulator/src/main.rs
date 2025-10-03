use std::{
    collections::{HashMap, VecDeque},
    net::SocketAddr,
    sync::Arc,
};

use anyhow::Result;
use axum::{
    extract::{Path, State},
    http::StatusCode,
    response::IntoResponse,
    routing::{get, post},
    Json, Router,
};
use serde::Deserialize;
use serde_json::Value;
use tokio::sync::Mutex;
use tracing::info;
use tracing_subscriber::EnvFilter;

#[derive(Clone, Default)]
struct AppState {
    queues: Arc<Mutex<HashMap<String, VecDeque<Value>>>>,
}

#[tokio::main]
async fn main() -> Result<()> {
    init_tracing();

    let state = AppState::default();

    let router = Router::new()
        .route("/health", get(health))
        .route("/queues/:queue/messages", post(enqueue))
        .route("/queues/:queue/dequeue", post(dequeue))
        .with_state(state);

    let addr: SocketAddr = "0.0.0.0:7075".parse()?;
    info!(%addr, "Service Bus emulator listening");

    axum::Server::bind(&addr)
        .serve(router.into_make_service())
        .with_graceful_shutdown(shutdown_signal())
        .await?;

    info!("Service Bus emulator shutdown complete");

    Ok(())
}

fn init_tracing() {
    let filter = EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new("info"));
    tracing_subscriber::fmt()
        .with_env_filter(filter)
        .compact()
        .init();
}

async fn shutdown_signal() {
    let _ = tokio::signal::ctrl_c().await;
}

async fn health() -> StatusCode {
    StatusCode::OK
}

#[derive(Deserialize)]
struct EnqueueRequest {
    body: Value,
}

async fn enqueue(
    Path(queue): Path<String>,
    State(state): State<AppState>,
    Json(request): Json<EnqueueRequest>,
) -> impl IntoResponse {
    let mut queues = state.queues.lock().await;
    queues
        .entry(queue.clone())
        .or_insert_with(VecDeque::new)
        .push_back(request.body);
    info!(queue = %queue, "enqueued message");
    StatusCode::ACCEPTED
}

async fn dequeue(Path(queue): Path<String>, State(state): State<AppState>) -> impl IntoResponse {
    let mut queues = state.queues.lock().await;
    let entry = queues.entry(queue.clone()).or_insert_with(VecDeque::new);
    match entry.pop_front() {
        Some(message) => {
            info!(queue = %queue, "dequeued message");
            Json(message).into_response()
        }
        None => StatusCode::NO_CONTENT.into_response(),
    }
}
