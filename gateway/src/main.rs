use std::{env, net::SocketAddr};

use gateway::{AppState, ServiceBusConfig, ServiceBusWorker, router};
use tokio::net::TcpListener;
use tracing::{info, warn};
use tracing_subscriber::EnvFilter;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    init_tracing();

    let state = AppState::default();

    let bus_config = ServiceBusConfig::from_env()
        .map_err(|error| -> Box<dyn std::error::Error> { Box::new(error) })?;

    if let Some(config) = bus_config {
        match ServiceBusWorker::from_config(config) {
            Ok(worker) => {
                worker.spawn(state.clone());
                info!("Service Bus admin listener started");
            }
            Err(error) => {
                warn!(%error, "failed to initialize Service Bus worker");
            }
        }
    } else {
        info!(
            "Service Bus configuration not provided; admin approvals will rely on the HTTP endpoint"
        );
    }

    let app = router(state);

    let host = env::var("HOST").unwrap_or_else(|_| "0.0.0.0".to_string());
    let port = env::var("PORT")
        .ok()
        .and_then(|value| value.parse::<u16>().ok())
        .unwrap_or(8080);
    let addr: SocketAddr = format!("{host}:{port}").parse()?;

    let listener = TcpListener::bind(addr).await?;
    info!(%addr, "EA counterparty service listening");

    axum::serve(listener, app)
        .with_graceful_shutdown(shutdown_signal())
        .await?;

    info!("shutdown complete");

    Ok(())
}

fn init_tracing() {
    let env_filter = EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new("info"));

    tracing_subscriber::fmt()
        .with_env_filter(env_filter)
        .compact()
        .init();
}

async fn shutdown_signal() {
    if let Err(error) = tokio::signal::ctrl_c().await {
        warn!(%error, "failed to install CTRL+C handler");
    }
}
