use std::{env, time::Duration};

use azure_core::{error::Error as AzureError, new_http_client, StatusCode};
use azure_messaging_servicebus::prelude::QueueClient;
use reqwest::{Client as HttpClient, StatusCode as HttpStatusCode, Url};
use serde::Deserialize;
use serde_json::Value;
use thiserror::Error;
use time::OffsetDateTime;
use tokio::{task::JoinHandle, time::sleep};
use tracing::{debug, error, info, warn};
use uuid::Uuid;

use crate::{
    AdminApprovalCommand, AdminCommand, AdminCommandError, AdminCommandOutcome,
    AdminRejectionCommand, ApiError, AppState, OutboxEventRequest, TradeOrderRequest,
};

const NAMESPACE_ENV: &str = "EA_SERVICE_BUS_NAMESPACE";
const QUEUE_ENV: &str = "EA_SERVICE_BUS_QUEUE";
const POLICY_ENV: &str = "EA_SERVICE_BUS_POLICY";
const KEY_ENV: &str = "EA_SERVICE_BUS_KEY";
const POLL_INTERVAL_ENV: &str = "EA_SERVICE_BUS_POLL_INTERVAL_SECS";
const EMULATOR_BASE_ENV: &str = "EA_SERVICE_BUS_EMULATOR_BASE_URL";

#[derive(Debug, Error)]
pub enum ServiceBusConfigError {
    #[error("environment variable {name} contains invalid UTF-8 characters")]
    InvalidUnicode {
        name: &'static str,
        #[source]
        source: env::VarError,
    },
    #[error(
        "Azure Service Bus configuration is incomplete; set {missing:?} or remove the remaining variables"
    )]
    Incomplete { missing: Vec<&'static str> },
    #[error("failed to parse {name}: {source}")]
    InvalidPollInterval {
        name: &'static str,
        #[source]
        source: std::num::ParseIntError,
    },
}

#[derive(Debug, Clone)]
pub struct ServiceBusConfig {
    pub namespace: String,
    pub queue: String,
    pub policy_name: String,
    pub policy_key: String,
    pub poll_interval: Duration,
}

impl ServiceBusConfig {
    pub fn from_env() -> Result<Option<Self>, ServiceBusConfigError> {
        let namespace = read_env(NAMESPACE_ENV)?;
        let queue = read_env(QUEUE_ENV)?;
        let policy_name = read_env(POLICY_ENV)?;
        let policy_key = read_env(KEY_ENV)?;

        let present = [&namespace, &queue, &policy_name, &policy_key]
            .iter()
            .filter(|value| value.is_some())
            .count();

        if present == 0 {
            return Ok(None);
        }

        if present != 4 {
            let missing = [
                (NAMESPACE_ENV, &namespace),
                (QUEUE_ENV, &queue),
                (POLICY_ENV, &policy_name),
                (KEY_ENV, &policy_key),
            ]
            .into_iter()
            .filter_map(|(name, value)| value.is_none().then_some(name))
            .collect();

            return Err(ServiceBusConfigError::Incomplete { missing });
        }

        let poll_interval = match env::var(POLL_INTERVAL_ENV) {
            Ok(value) => {
                let secs: u64 =
                    value
                        .parse()
                        .map_err(|source| ServiceBusConfigError::InvalidPollInterval {
                            name: POLL_INTERVAL_ENV,
                            source,
                        })?;
                Duration::from_secs(secs.max(1))
            }
            Err(env::VarError::NotPresent) => Duration::from_secs(5),
            Err(error) => {
                return Err(ServiceBusConfigError::InvalidUnicode {
                    name: POLL_INTERVAL_ENV,
                    source: error,
                });
            }
        };

        Ok(Some(ServiceBusConfig {
            namespace: namespace.expect("namespace present"),
            queue: queue.expect("queue present"),
            policy_name: policy_name.expect("policy name present"),
            policy_key: policy_key.expect("policy key present"),
            poll_interval,
        }))
    }
}

fn read_env(name: &'static str) -> Result<Option<String>, ServiceBusConfigError> {
    match env::var(name) {
        Ok(value) => Ok(Some(value)),
        Err(env::VarError::NotPresent) => Ok(None),
        Err(error) => Err(ServiceBusConfigError::InvalidUnicode {
            name,
            source: error,
        }),
    }
}

pub struct ServiceBusWorker {
    backend: ServiceBusBackend,
    poll_interval: Duration,
}

enum ServiceBusBackend {
    Azure {
        client: QueueClient,
        queue_name: String,
    },
    Emulator {
        client: HttpClient,
        queue_name: String,
        dequeue_url: Url,
    },
}

#[derive(Debug, Error)]
pub enum ServiceBusWorkerInitError {
    #[error(transparent)]
    Azure(#[from] AzureError),
    #[error("invalid Service Bus emulator base URL: {0}")]
    InvalidEmulatorUrl(url::ParseError),
}

impl ServiceBusWorker {
    pub fn from_config(config: ServiceBusConfig) -> Result<Self, ServiceBusWorkerInitError> {
        if let Ok(base_url) = env::var(EMULATOR_BASE_ENV) {
            let client = HttpClient::new();
            let base =
                Url::parse(&base_url).map_err(ServiceBusWorkerInitError::InvalidEmulatorUrl)?;
            let dequeue_url = base
                .join(&format!("queues/{}/dequeue", config.queue))
                .map_err(ServiceBusWorkerInitError::InvalidEmulatorUrl)?;

            return Ok(Self {
                backend: ServiceBusBackend::Emulator {
                    client,
                    queue_name: config.queue,
                    dequeue_url,
                },
                poll_interval: config.poll_interval,
            });
        }

        let http_client = new_http_client();
        let queue_name = config.queue.clone();
        let client = QueueClient::new(
            http_client,
            config.namespace,
            queue_name.clone(),
            config.policy_name,
            config.policy_key,
        )
        .map_err(ServiceBusWorkerInitError::Azure)?;

        Ok(Self {
            backend: ServiceBusBackend::Azure { client, queue_name },
            poll_interval: config.poll_interval,
        })
    }

    pub fn spawn(self, state: AppState) -> JoinHandle<()> {
        match self.backend {
            ServiceBusBackend::Azure { client, queue_name } => {
                let poll_interval = self.poll_interval;
                tokio::spawn(async move {
                    loop {
                        match client.peek_lock_message2(Some(poll_interval)).await {
                            Ok(lock) => {
                                if lock.status() == &StatusCode::NoContent {
                                    sleep(poll_interval).await;
                                    continue;
                                }

                                let body = lock.body();
                                if body.trim().is_empty() {
                                    debug!(queue = %queue_name, "received empty admin message");
                                    if let Err(error) = lock.delete_message().await {
                                        warn!(queue = %queue_name, %error, "failed to delete empty admin message");
                                    }
                                    continue;
                                }

                                match serde_json::from_str::<AdminEnqueueRequest>(&body) {
                                    Ok(envelope) => {
                                        let result =
                                            process_envelope(&state, envelope, &queue_name).await;

                                        match result {
                                            Ok(()) => {
                                                if let Err(error) = lock.delete_message().await {
                                                    warn!(
                                                        queue = %queue_name,
                                                        %error,
                                                        "failed to delete processed admin message"
                                                    );
                                                }
                                            }
                                            Err(error) => match &error {
                                                MessageHandlingError::Admin { account, .. } => {
                                                    warn!(
                                                        queue = %queue_name,
                                                        account = %account,
                                                        %error,
                                                        "failed to apply admin command from Service Bus",
                                                    );
                                                }
                                                MessageHandlingError::Api {
                                                    account,
                                                    session,
                                                    source,
                                                } => {
                                                    error!(
                                                        queue = %queue_name,
                                                        account = %account,
                                                        session = %session,
                                                        %source,
                                                        "failed to deliver admin request from Service Bus",
                                                    );
                                                }
                                            },
                                        }
                                    }
                                    Err(error) => {
                                        warn!(
                                            queue = %queue_name,
                                            %error,
                                            "failed to deserialize admin message"
                                        );
                                    }
                                }
                            }
                            Err(error) => {
                                warn!(
                                    queue = %queue_name,
                                    %error,
                                    "failed to receive admin message from Service Bus"
                                );
                                sleep(poll_interval).await;
                            }
                        }
                    }
                })
            }
            ServiceBusBackend::Emulator {
                client,
                queue_name,
                dequeue_url,
            } => {
                let poll_interval = self.poll_interval;
                tokio::spawn(async move {
                    loop {
                        match client.post(dequeue_url.clone()).send().await {
                            Ok(response) => {
                                if response.status() == HttpStatusCode::NO_CONTENT {
                                    sleep(poll_interval).await;
                                    continue;
                                }

                                if !response.status().is_success() {
                                    let status = response.status();
                                    let body = response.text().await.unwrap_or_default();
                                    warn!(
                                        queue = %queue_name,
                                        %status,
                                        body,
                                        "failed to dequeue admin message from emulator"
                                    );
                                    sleep(poll_interval).await;
                                    continue;
                                }

                                match response.json::<AdminEnqueueRequest>().await {
                                    Ok(envelope) => {
                                        if let Err(error) =
                                            process_envelope(&state, envelope, &queue_name).await
                                        {
                                            match error {
                                                MessageHandlingError::Admin { account, source } => {
                                                    warn!(
                                                        queue = %queue_name,
                                                        account = %account,
                                                        %source,
                                                        "failed to apply admin command from emulator",
                                                    );
                                                }
                                                MessageHandlingError::Api {
                                                    account,
                                                    session,
                                                    source,
                                                } => {
                                                    error!(
                                                        queue = %queue_name,
                                                        account = %account,
                                                        session = %session,
                                                        %source,
                                                        "failed to deliver admin request from emulator",
                                                    );
                                                }
                                            }
                                        }
                                    }
                                    Err(error) => {
                                        warn!(
                                            queue = %queue_name,
                                            %error,
                                            "failed to parse admin message from emulator"
                                        );
                                    }
                                }
                            }
                            Err(error) => {
                                warn!(
                                    queue = %queue_name,
                                    %error,
                                    "failed to fetch admin message from emulator"
                                );
                            }
                        }

                        sleep(poll_interval).await;
                    }
                })
            }
        }
    }
}
#[derive(Debug, Error)]
pub(crate) enum MessageHandlingError {
    #[error("{source}")]
    Admin {
        account: String,
        #[source]
        source: AdminCommandError,
    },
    #[error("{source}")]
    Api {
        account: String,
        session: Uuid,
        #[source]
        source: ApiError,
    },
}

async fn handle_command(
    state: &AppState,
    command: AdminCommand,
    queue_name: &str,
    account: &str,
) -> Result<(), AdminCommandError> {
    match state.apply_admin_command(command).await {
        Ok(AdminCommandOutcome::SessionAuthenticated(outcome)) => {
            info!(
                queue = %queue_name,
                account = %account,
                session = %outcome.session_id,
                "processed Service Bus approval",
            );
            Ok(())
        }
        Ok(AdminCommandOutcome::SessionRejected(outcome)) => {
            info!(
                queue = %queue_name,
                account = %account,
                session = %outcome.session_id,
                reason = ?outcome.reason,
                "processed Service Bus rejection",
            );
            Ok(())
        }
        Err(error) => Err(error),
    }
}

#[derive(Debug, Deserialize)]
#[serde(tag = "type", rename_all = "camelCase")]
pub(crate) enum AdminEnqueueRequest {
    AuthApproval(AuthApprovalMessage),
    AuthReject(AuthRejectMessage),
    QueueOutboxEvent(OutboxEventMessage),
    TradeOrder(TradeOrderMessage),
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub(crate) struct AuthApprovalMessage {
    account_id: String,
    session_id: Uuid,
    #[serde(alias = "authKeyHash")]
    auth_key_fingerprint: String,
    #[serde(default)]
    approved_by: Option<String>,
    #[serde(default)]
    expires_at: Option<OffsetDateTime>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub(crate) struct AuthRejectMessage {
    account_id: String,
    session_id: Uuid,
    #[serde(alias = "authKeyHash")]
    auth_key_fingerprint: String,
    #[serde(default)]
    rejected_by: Option<String>,
    #[serde(default)]
    reason: Option<String>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub(crate) struct OutboxEventMessage {
    account_id: String,
    session_id: Uuid,
    event_type: String,
    #[serde(default)]
    payload: Value,
    #[serde(default = "default_requires_ack_true")]
    requires_ack: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub(crate) struct TradeOrderMessage {
    account_id: String,
    session_id: Uuid,
    #[serde(flatten)]
    command: TradeOrderRequest,
}

const fn default_requires_ack_true() -> bool {
    true
}

pub(crate) async fn apply_envelope(
    state: &AppState,
    envelope: AdminEnqueueRequest,
) -> Result<(), MessageHandlingError> {
    process_envelope(state, envelope, "http-admin").await
}

async fn process_envelope(
    state: &AppState,
    envelope: AdminEnqueueRequest,
    queue_name: &str,
) -> Result<(), MessageHandlingError> {
    match envelope {
        AdminEnqueueRequest::AuthApproval(message) => {
            let account = message.account_id.clone();
            let session_id = message.session_id;
            let command = AdminCommand::Approve(AdminApprovalCommand {
                account_id: message.account_id,
                session_id,
                auth_key_fingerprint: message.auth_key_fingerprint,
                approved_by: message.approved_by,
                expires_at: message.expires_at,
            });

            handle_command(state, command, queue_name, &account)
                .await
                .map_err(|source| MessageHandlingError::Admin { account, source })
        }
        AdminEnqueueRequest::AuthReject(message) => {
            let account = message.account_id.clone();
            let session_id = message.session_id;
            let command = AdminCommand::Reject(AdminRejectionCommand {
                account_id: message.account_id,
                session_id,
                auth_key_fingerprint: message.auth_key_fingerprint,
                rejected_by: message.rejected_by,
                reason: message.reason,
            });

            handle_command(state, command, queue_name, &account)
                .await
                .map_err(|source| MessageHandlingError::Admin { account, source })
        }
        AdminEnqueueRequest::QueueOutboxEvent(message) => {
            process_outbox_event(state, message, queue_name).await
        }
        AdminEnqueueRequest::TradeOrder(message) => {
            process_trade_order(state, message, queue_name).await
        }
    }
}

async fn process_outbox_event(
    state: &AppState,
    message: OutboxEventMessage,
    queue_name: &str,
) -> Result<(), MessageHandlingError> {
    let OutboxEventMessage {
        account_id,
        session_id,
        event_type,
        payload,
        requires_ack,
    } = message;

    let request = OutboxEventRequest {
        event_type: event_type.clone(),
        payload,
        requires_ack,
    };

    let response = state
        .enqueue_outbox_event(&account_id, session_id, request)
        .await
        .map_err(|source| MessageHandlingError::Api {
            account: account_id.clone(),
            session: session_id,
            source,
        })?;

    info!(
        queue = %queue_name,
        account = %account_id,
        session = %session_id,
        event = %response.event_id,
        event_type = %event_type,
        pending = response.pending_session,
        "queued outbox event from Service Bus",
    );

    Ok(())
}

async fn process_trade_order(
    state: &AppState,
    message: TradeOrderMessage,
    queue_name: &str,
) -> Result<(), MessageHandlingError> {
    let TradeOrderMessage {
        account_id,
        session_id,
        command,
    } = message;

    let queued = state
        .enqueue_trade_command(&account_id, session_id, command)
        .await
        .map_err(|source| MessageHandlingError::Api {
            account: account_id.clone(),
            session: session_id,
            source,
        })?;

    info!(
        queue = %queue_name,
        account = %account_id,
        session = %session_id,
        event = %queued.event_id,
        command = %queued.command_id,
        instrument = %queued.instrument,
        command_type = ?queued.command_type,
        pending = queued.pending_session,
        "queued trade command from Service Bus",
    );

    Ok(())
}
