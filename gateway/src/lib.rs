use std::{collections::HashMap, sync::Arc};

use axum::{
    Json, Router,
    body::Body,
    extract::{Path, Query, State},
    http::{HeaderMap, HeaderName, HeaderValue, StatusCode, header},
    response::{IntoResponse, Response},
    routing::{delete, get, post},
};
use serde::{Deserialize, Serialize};
use serde_json::{Map, Value, json};
use sha2::{Digest, Sha256};
use thiserror::Error;
use time::OffsetDateTime;
use tokio::sync::Mutex;
use tracing::{debug, info, warn};
use uuid::Uuid;

mod admin;

pub use admin::{ServiceBusConfig, ServiceBusConfigError, ServiceBusWorker};

/// Builds the application router for the EA counterparty service.
pub fn router(state: AppState) -> Router {
    Router::new()
        .route("/trade-agent/v1/health", get(health_handler))
        .route("/trade-agent/v1/sessions", post(create_session))
        .route("/trade-agent/v1/sessions/current", delete(delete_session))
        .route(
            "/trade-agent/v1/sessions/current/inbox",
            post(ingest_inbox_events),
        )
        .route(
            "/trade-agent/v1/sessions/current/outbox",
            get(fetch_outbox_events),
        )
        .route(
            "/trade-agent/v1/sessions/current/outbox/:event_id/ack",
            post(acknowledge_outbox_event),
        )
        .route(
            "/trade-agent/v1/sessions/:session_id/approve",
            post(approve_session),
        )
        .route(
            "/trade-agent/v1/sessions/:session_id/outbox",
            post(queue_outbox_event),
        )
        .with_state(state)
}

/// Shared application state guarded by a mutex for simplicity.
#[derive(Clone, Default)]
pub struct AppState {
    inner: Arc<Mutex<SharedState>>,
}

#[derive(Default)]
struct SharedState {
    sessions: HashMap<String, AccountSessions>,
    idempotency: HashMap<String, StoredResponse>,
}

#[derive(Debug, Clone)]
pub struct AdminApprovalCommand {
    pub account_id: String,
    pub session_id: Uuid,
    pub auth_key_fingerprint: String,
    pub approved_by: Option<String>,
    pub expires_at: Option<OffsetDateTime>,
}

#[derive(Debug, Clone)]
pub struct AdminRejectionCommand {
    pub account_id: String,
    pub session_id: Uuid,
    pub auth_key_fingerprint: String,
    pub rejected_by: Option<String>,
    pub reason: Option<String>,
}

#[derive(Debug, Clone)]
pub enum AdminCommand {
    Approve(AdminApprovalCommand),
    Reject(AdminRejectionCommand),
}

#[derive(Debug, Error)]
pub enum AdminCommandError {
    #[error("no active session for the supplied account")]
    SessionMissing,
    #[error("the supplied session id does not match the active session")]
    SessionMismatch,
    #[error("the provided authentication key is invalid")]
    AuthenticationFailed,
    #[error("the session has been terminated and cannot be modified")]
    SessionTerminated,
    #[error("authentication key must not be empty")]
    AuthenticationKeyEmpty,
}

#[derive(Default)]
struct AccountSessions {
    sessions_by_token: HashMap<Uuid, SessionRecord>,
    active_index: HashMap<String, Uuid>,
    session_index: HashMap<Uuid, Uuid>,
    preapproved: HashMap<String, PreapprovalRecord>,
}

impl AccountSessions {
    fn insert(&mut self, session: SessionRecord) {
        let token = session.session_token;
        let session_id = session.session_id;
        let auth_hash = session.auth_key_hash.clone();

        self.active_index.insert(auth_hash.clone(), token);
        self.session_index.insert(session_id, token);
        self.sessions_by_token.insert(token, session);
    }

    fn preempt_existing(&mut self, auth_hash: &str) -> Option<Uuid> {
        let token = self.active_index.remove(auth_hash)?;
        let session = self.sessions_by_token.get_mut(&token)?;
        session.mark_terminated(TerminationReason::Preempted);
        Some(session.session_id)
    }

    fn get_mut_by_token(&mut self, token: &Uuid) -> Option<&mut SessionRecord> {
        self.sessions_by_token.get_mut(token)
    }

    fn get_mut_by_session_id(&mut self, session_id: &Uuid) -> Option<&mut SessionRecord> {
        let token = self.session_index.get(session_id)?;
        self.sessions_by_token.get_mut(token)
    }

    fn remove_by_token(&mut self, token: &Uuid) -> Option<SessionRecord> {
        let session = self.sessions_by_token.remove(token)?;
        self.session_index.remove(&session.session_id);
        if matches!(
            self.active_index.get(&session.auth_key_hash),
            Some(stored) if stored == token
        ) {
            self.active_index.remove(&session.auth_key_hash);
        }
        Some(session)
    }

    fn remove_from_active_index(&mut self, auth_hash: &str, token: Uuid) {
        if matches!(self.active_index.get(auth_hash), Some(stored) if stored == &token) {
            self.active_index.remove(auth_hash);
        }
    }

    fn is_empty(&self) -> bool {
        self.sessions_by_token.is_empty() && self.preapproved.is_empty()
    }

    fn register_preapproval(&mut self, fingerprint: String, record: PreapprovalRecord) {
        self.preapproved.insert(fingerprint, record);
    }

    fn consume_preapproval(&mut self, fingerprint: &str) -> Option<PreapprovalRecord> {
        let record = self.preapproved.remove(fingerprint)?;
        if record.is_expired(current_time()) {
            None
        } else {
            Some(record)
        }
    }
}

#[derive(Debug, Clone)]
struct PreapprovalRecord {
    approved_by: Option<String>,
    expires_at: Option<OffsetDateTime>,
}

impl PreapprovalRecord {
    fn is_expired(&self, now: OffsetDateTime) -> bool {
        match self.expires_at {
            Some(expiry) => expiry <= now,
            None => false,
        }
    }
}

impl AppState {
    async fn stored_response(&self, key: &str) -> Option<StoredResponse> {
        self.inner.lock().await.idempotency.get(key).cloned()
    }

    async fn store_response(&self, key: String, stored: StoredResponse) {
        self.inner.lock().await.idempotency.insert(key, stored);
    }

    pub async fn preapprove_session_key(
        &self,
        account: &str,
        auth_method: AuthMethod,
        authentication_key: &str,
        approved_by: Option<String>,
        expires_at: Option<OffsetDateTime>,
    ) -> Result<(), AdminCommandError> {
        if authentication_key.trim().is_empty() {
            return Err(AdminCommandError::AuthenticationKeyEmpty);
        }

        let fingerprint = hash_secret(auth_method, authentication_key, account);
        let mut inner = self.inner.lock().await;
        let account_sessions = inner.sessions.entry(account.to_string()).or_default();

        match approved_by.as_deref() {
            Some(operator) if !operator.is_empty() => {
                info!(
                    account = %account,
                    operator,
                    auth_method = ?auth_method,
                    "registered pre-approved authentication key",
                );
            }
            _ => info!(
                account = %account,
                auth_method = ?auth_method,
                "registered pre-approved authentication key",
            ),
        }

        account_sessions.register_preapproval(
            fingerprint,
            PreapprovalRecord {
                approved_by,
                expires_at,
            },
        );

        Ok(())
    }

    pub async fn promote_session_with_secret(
        &self,
        account: &str,
        session_id: Uuid,
        authentication_key: &str,
    ) -> Result<SessionPromotionResponse, AdminCommandError> {
        if authentication_key.trim().is_empty() {
            return Err(AdminCommandError::AuthenticationKeyEmpty);
        }

        let fingerprint = {
            let mut inner = self.inner.lock().await;
            let account_sessions = inner
                .sessions
                .get_mut(account)
                .ok_or(AdminCommandError::SessionMissing)?;
            let session = account_sessions
                .get_mut_by_session_id(&session_id)
                .ok_or(AdminCommandError::SessionMismatch)?;
            hash_secret(session.auth_method, authentication_key, account)
        };

        self.promote_session_internal(account, session_id, fingerprint, None)
            .await
    }

    pub async fn promote_session_with_fingerprint(
        &self,
        account: &str,
        session_id: Uuid,
        fingerprint: &str,
        operator: Option<&str>,
    ) -> Result<SessionPromotionResponse, AdminCommandError> {
        self.promote_session_internal(account, session_id, fingerprint.to_string(), operator)
            .await
    }

    pub async fn reject_session(
        &self,
        account: &str,
        session_id: Uuid,
        fingerprint: &str,
        reason: Option<String>,
        rejected_by: Option<String>,
    ) -> Result<SessionRejectionResponse, AdminCommandError> {
        let mut inner = self.inner.lock().await;
        let account_sessions = inner
            .sessions
            .get_mut(account)
            .ok_or(AdminCommandError::SessionMissing)?;
        let (outcome, auth_hash, session_token) = {
            let session = account_sessions
                .get_mut_by_session_id(&session_id)
                .ok_or(AdminCommandError::SessionMismatch)?;

            if !session.verify_secret(fingerprint) {
                return Err(AdminCommandError::AuthenticationFailed);
            }

            let auth_hash = session.auth_key_hash.clone();
            let session_token = session.session_token;
            let outcome = session.reject(reason.clone(), rejected_by.clone());
            (outcome, auth_hash, session_token)
        };

        if outcome.already_terminated {
            debug!(
                account = %account,
                session = %session_id,
                "reject command ignored; session already terminated"
            );
        } else {
            account_sessions.remove_from_active_index(&auth_hash, session_token);
            match rejected_by.as_deref() {
                Some(operator) if !operator.is_empty() => {
                    warn!(
                        account = %account,
                        session = %session_id,
                        operator,
                        reason = ?reason,
                        "session rejected"
                    );
                }
                _ => {
                    warn!(
                        account = %account,
                        session = %session_id,
                        reason = ?reason,
                        "session rejected"
                    );
                }
            }
        }

        Ok(outcome.response)
    }

    pub async fn apply_admin_command(
        &self,
        command: AdminCommand,
    ) -> Result<AdminCommandOutcome, AdminCommandError> {
        match command {
            AdminCommand::Approve(command) => {
                let AdminApprovalCommand {
                    account_id,
                    session_id,
                    auth_key_fingerprint,
                    approved_by,
                    expires_at: _,
                } = command;

                let response = self
                    .promote_session_with_fingerprint(
                        &account_id,
                        session_id,
                        &auth_key_fingerprint,
                        approved_by.as_deref(),
                    )
                    .await?;

                Ok(AdminCommandOutcome::SessionAuthenticated(response))
            }
            AdminCommand::Reject(command) => {
                let AdminRejectionCommand {
                    account_id,
                    session_id,
                    auth_key_fingerprint,
                    rejected_by,
                    reason,
                } = command;

                let response = self
                    .reject_session(
                        &account_id,
                        session_id,
                        &auth_key_fingerprint,
                        reason,
                        rejected_by,
                    )
                    .await?;

                Ok(AdminCommandOutcome::SessionRejected(response))
            }
        }
    }

    async fn promote_session_internal(
        &self,
        account: &str,
        session_id: Uuid,
        fingerprint: String,
        operator: Option<&str>,
    ) -> Result<SessionPromotionResponse, AdminCommandError> {
        let mut inner = self.inner.lock().await;
        let account_sessions = inner
            .sessions
            .get_mut(account)
            .ok_or(AdminCommandError::SessionMissing)?;
        let session = account_sessions
            .get_mut_by_session_id(&session_id)
            .ok_or(AdminCommandError::SessionMismatch)?;

        let was_pending = session.status.is_pending();
        let response = session.promote(&fingerprint)?;

        if was_pending && response.status == SessionStatus::Authenticated {
            match operator {
                Some(operator) if !operator.is_empty() => {
                    info!(
                        account = %account,
                        session = %session_id,
                        operator,
                        "session authenticated"
                    );
                }
                _ => {
                    info!(account = %account, session = %session_id, "session authenticated");
                }
            }
        }

        Ok(response)
    }
}

/// Health status payload emitted by the service.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HealthStatus {
    /// Name of the component being checked.
    pub component: &'static str,
    /// Human-readable description of the current state.
    pub message: &'static str,
    /// Indicates whether the component is healthy.
    pub healthy: bool,
}

impl HealthStatus {
    /// Creates a successful health check response for the provided component.
    pub const fn ok(component: &'static str) -> Self {
        Self {
            component,
            message: "ok",
            healthy: true,
        }
    }
}

/// Returns the health status of the gateway service.
pub fn health_check() -> HealthStatus {
    HealthStatus::ok("gateway")
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct ErrorBody {
    code: &'static str,
    message: String,
}

#[derive(Debug, Error)]
#[error("{message}")]
struct ApiError {
    status: StatusCode,
    code: &'static str,
    message: String,
}

impl ApiError {
    fn bad_request(code: &'static str, message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::BAD_REQUEST,
            code,
            message: message.into(),
        }
    }

    fn unauthorized(code: &'static str, message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::UNAUTHORIZED,
            code,
            message: message.into(),
        }
    }

    fn forbidden(code: &'static str, message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::FORBIDDEN,
            code,
            message: message.into(),
        }
    }

    fn conflict(code: &'static str, message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::CONFLICT,
            code,
            message: message.into(),
        }
    }

    fn not_found(code: &'static str, message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::NOT_FOUND,
            code,
            message: message.into(),
        }
    }

    fn internal(message: impl Into<String>) -> Self {
        Self {
            status: StatusCode::INTERNAL_SERVER_ERROR,
            code: "internal_error",
            message: message.into(),
        }
    }
}

impl IntoResponse for ApiError {
    fn into_response(self) -> Response {
        let body = Json(ErrorBody {
            code: self.code,
            message: self.message,
        });
        (self.status, body).into_response()
    }
}

#[derive(Clone)]
struct StoredResponse {
    status: StatusCode,
    body: Option<Value>,
    headers: Vec<(String, String)>,
}

impl StoredResponse {
    fn from_json<T>(status: StatusCode, payload: &T) -> Result<Self, serde_json::Error>
    where
        T: Serialize,
    {
        Ok(Self {
            status,
            body: Some(serde_json::to_value(payload)?),
            headers: Vec::new(),
        })
    }

    fn empty(status: StatusCode) -> Self {
        Self {
            status,
            body: None,
            headers: Vec::new(),
        }
    }

    fn into_response(self) -> Response {
        let mut response = if let Some(body) = self.body {
            (self.status, Json(body)).into_response()
        } else {
            Response::builder()
                .status(self.status)
                .body(Body::empty())
                .unwrap_or_else(|_| Response::new(Body::empty()))
        };

        for (name, value) in self.headers {
            if let (Ok(name), Ok(value)) = (
                HeaderName::from_bytes(name.as_bytes()),
                HeaderValue::from_str(&value),
            ) {
                response.headers_mut().insert(name, value);
            }
        }

        response
    }
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum SessionStatus {
    Pending,
    Authenticated,
    Terminated,
}

impl SessionStatus {
    const fn is_pending(self) -> bool {
        matches!(self, SessionStatus::Pending)
    }
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
pub enum AuthMethod {
    AccountSessionKey,
    PreSharedKey,
}

impl Default for AuthMethod {
    fn default() -> Self {
        AuthMethod::AccountSessionKey
    }
}

impl AuthMethod {
    const fn storage_key(self) -> &'static str {
        match self {
            AuthMethod::AccountSessionKey => "account_session_key",
            AuthMethod::PreSharedKey => "pre_shared_key",
        }
    }
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateRequest {
    #[serde(default)]
    auth_method: AuthMethod,
    authentication_key: String,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateResponse {
    session_id: Uuid,
    session_token: Uuid,
    status: SessionStatus,
    auth_method: AuthMethod,
    pending: bool,
    created_at: OffsetDateTime,
    last_heartbeat_at: Option<OffsetDateTime>,
    previous_session_terminated: Option<Uuid>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxBatch {
    events: Vec<InboxEvent>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxEvent {
    event_type: String,
    #[serde(default)]
    payload: Value,
    #[serde(default)]
    occurred_at: Option<OffsetDateTime>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct InboundEventRecord {
    id: Uuid,
    event_type: String,
    payload: Value,
    occurred_at: Option<OffsetDateTime>,
    received_at: OffsetDateTime,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxEventRequest {
    event_type: String,
    #[serde(default)]
    payload: Value,
    #[serde(default = "default_requires_ack")]
    requires_ack: bool,
}

const fn default_requires_ack() -> bool {
    true
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct OutboundEvent {
    id: Uuid,
    sequence: u64,
    event_type: String,
    payload: Value,
    enqueued_at: OffsetDateTime,
    requires_ack: bool,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct OutboxResponse {
    session_id: Uuid,
    pending: bool,
    events: Vec<OutboundEvent>,
    retry_after_ms: u64,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct AckResponse {
    acknowledged_event_id: Uuid,
    remaining_outbox_depth: usize,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct InboxResponse {
    accepted: usize,
    pending_session: bool,
}

#[derive(Debug, Clone, Serialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub struct SessionPromotionResponse {
    session_id: Uuid,
    status: SessionStatus,
    pending: bool,
    message: String,
}

#[derive(Debug, Clone, Serialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub struct SessionRejectionResponse {
    session_id: Uuid,
    status: SessionStatus,
    message: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    reason: Option<String>,
}

#[derive(Debug, Clone)]
pub enum AdminCommandOutcome {
    SessionAuthenticated(SessionPromotionResponse),
    SessionRejected(SessionRejectionResponse),
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct OutboxEnqueueResponse {
    session_id: Uuid,
    event_id: Uuid,
    sequence: u64,
    pending_session: bool,
}

#[derive(Debug, Deserialize)]
struct OutboxQuery {
    #[serde(default)]
    cursor: Option<u64>,
    #[serde(default)]
    limit: Option<usize>,
}

struct SessionRecord {
    session_id: Uuid,
    session_token: Uuid,
    status: SessionStatus,
    auth_method: AuthMethod,
    auth_key_hash: String,
    created_at: OffsetDateTime,
    updated_at: OffsetDateTime,
    last_heartbeat_at: Option<OffsetDateTime>,
    next_sequence: u64,
    outbox: Vec<OutboundEvent>,
    inbox_log: Vec<InboundEventRecord>,
}

struct SessionRejectionOutcome {
    response: SessionRejectionResponse,
    already_terminated: bool,
}

enum TerminationReason {
    Preempted,
    Rejected {
        rejected_by: Option<String>,
        reason: Option<String>,
    },
}

impl TerminationReason {
    fn into_payload(self, terminated_at: OffsetDateTime) -> Value {
        match self {
            TerminationReason::Preempted => json!({
                "reason": "session_preempted",
                "terminatedAt": terminated_at,
            }),
            TerminationReason::Rejected {
                rejected_by,
                reason,
            } => {
                let mut payload = Map::new();
                payload.insert(
                    "reason".to_string(),
                    Value::String("session_rejected".to_string()),
                );
                payload.insert("terminatedAt".to_string(), json!(terminated_at));
                if let Some(reason) = reason {
                    if !reason.is_empty() {
                        payload.insert("details".to_string(), Value::String(reason));
                    }
                }
                if let Some(operator) = rejected_by {
                    if !operator.is_empty() {
                        payload.insert("rejectedBy".to_string(), Value::String(operator));
                    }
                }
                Value::Object(payload)
            }
        }
    }
}

impl SessionRecord {
    fn new(auth_method: AuthMethod, auth_key_hash: String) -> Self {
        let now = current_time();
        Self {
            session_id: Uuid::new_v4(),
            session_token: Uuid::new_v4(),
            status: SessionStatus::Pending,
            auth_method,
            auth_key_hash,
            created_at: now,
            updated_at: now,
            last_heartbeat_at: None,
            next_sequence: 1,
            outbox: Vec::new(),
            inbox_log: Vec::new(),
        }
    }

    fn to_create_response(&self, previous_session: Option<Uuid>) -> SessionCreateResponse {
        SessionCreateResponse {
            session_id: self.session_id,
            session_token: self.session_token,
            status: self.status,
            auth_method: self.auth_method,
            pending: self.status.is_pending(),
            created_at: self.created_at,
            last_heartbeat_at: self.last_heartbeat_at,
            previous_session_terminated: previous_session,
        }
    }

    fn verify_secret(&self, candidate_hash: &str) -> bool {
        self.auth_key_hash == candidate_hash
    }

    fn promote(
        &mut self,
        fingerprint: &str,
    ) -> Result<SessionPromotionResponse, AdminCommandError> {
        if !self.verify_secret(fingerprint) {
            return Err(AdminCommandError::AuthenticationFailed);
        }

        if self.status == SessionStatus::Terminated {
            return Err(AdminCommandError::SessionTerminated);
        }

        if self.status == SessionStatus::Authenticated {
            return Ok(SessionPromotionResponse {
                session_id: self.session_id,
                status: self.status,
                pending: false,
                message: "session already authenticated".to_string(),
            });
        }

        self.mark_authenticated();
        self.enqueue_init_ack();

        Ok(SessionPromotionResponse {
            session_id: self.session_id,
            status: self.status,
            pending: false,
            message: "session authenticated".to_string(),
        })
    }

    fn reject(
        &mut self,
        reason: Option<String>,
        rejected_by: Option<String>,
    ) -> SessionRejectionOutcome {
        let already_terminated = self.status == SessionStatus::Terminated;

        if !already_terminated {
            self.mark_terminated(TerminationReason::Rejected {
                rejected_by: rejected_by.clone(),
                reason: reason.clone(),
            });
        }

        let message = if already_terminated {
            "session already terminated"
        } else {
            "session rejected"
        };

        SessionRejectionOutcome {
            response: SessionRejectionResponse {
                session_id: self.session_id,
                status: self.status,
                message: message.to_string(),
                reason,
            },
            already_terminated,
        }
    }

    fn mark_authenticated(&mut self) {
        self.status = SessionStatus::Authenticated;
        self.updated_at = current_time();
    }

    fn mark_terminated(&mut self, reason: TerminationReason) {
        if self.status == SessionStatus::Terminated {
            return;
        }

        self.status = SessionStatus::Terminated;
        let terminated_at = current_time();
        let payload = reason.into_payload(terminated_at);

        let request = OutboxEventRequest {
            event_type: "ShutdownNotice".to_string(),
            payload,
            requires_ack: true,
        };

        self.enqueue_outbox(request);
    }

    fn enqueue_outbox(&mut self, event: OutboxEventRequest) -> OutboundEvent {
        let enqueued_at = current_time();
        let outbound = OutboundEvent {
            id: Uuid::new_v4(),
            sequence: self.next_sequence,
            event_type: event.event_type,
            payload: event.payload,
            enqueued_at,
            requires_ack: event.requires_ack,
        };
        self.next_sequence += 1;
        self.outbox.push(outbound.clone());
        self.updated_at = enqueued_at;
        outbound
    }

    fn enqueue_init_ack(&mut self) {
        let request = OutboxEventRequest {
            event_type: "InitAck".to_string(),
            payload: json!({
                "message": "Session authenticated",
            }),
            requires_ack: true,
        };
        self.enqueue_outbox(request);
    }

    fn apply_outbox_ack(&mut self, payload: &Value) {
        let Some(event_id_value) = payload.get("eventId") else {
            debug!("outbox ack missing eventId field");
            return;
        };

        let Some(event_id) = event_id_value.as_str() else {
            debug!(?event_id_value, "outbox ack eventId is not a string");
            return;
        };

        let Ok(event_uuid) = Uuid::parse_str(event_id) else {
            debug!(%event_id, "failed to parse outbox ack eventId");
            return;
        };

        let sequence = payload.get("sequence").and_then(Value::as_u64);
        let removed = self.acknowledge_outbox(event_uuid);

        if removed {
            debug!(%event_uuid, sequence, "acknowledged outbox event via inbox");
        } else {
            debug!(%event_uuid, sequence, "outbox ack did not match a pending event");
        }
    }

    fn capture_inbox(&mut self, batch: Vec<InboxEvent>) -> Vec<InboundEventRecord> {
        let mut captured = Vec::with_capacity(batch.len());
        for event in batch {
            let received_at = current_time();
            let event_type_lower = event.event_type.to_ascii_lowercase();
            if event_type_lower.contains("heartbeat") {
                self.last_heartbeat_at = Some(received_at);
            }

            if event_type_lower == "outboxack" {
                self.apply_outbox_ack(&event.payload);
            }

            let record = InboundEventRecord {
                id: Uuid::new_v4(),
                event_type: event.event_type,
                payload: event.payload,
                occurred_at: event.occurred_at,
                received_at,
            };
            self.inbox_log.push(record.clone());
            captured.push(record);
        }

        if !captured.is_empty() {
            self.updated_at = current_time();
        }

        captured
    }

    fn acknowledge_outbox(&mut self, event_id: Uuid) -> bool {
        let initial_len = self.outbox.len();
        self.outbox.retain(|event| event.id != event_id);
        let removed = initial_len != self.outbox.len();
        if removed {
            self.updated_at = current_time();
        }
        removed
    }

    fn events_after(
        &self,
        cursor: u64,
        limit: Option<usize>,
        include_when_pending: bool,
    ) -> Vec<OutboundEvent> {
        if self.status.is_pending() && !include_when_pending {
            return Vec::new();
        }

        let iter = self.outbox.iter().filter(|event| event.sequence > cursor);
        match limit {
            Some(limit) => iter.take(limit).cloned().collect(),
            None => iter.cloned().collect(),
        }
    }
}

async fn health_handler() -> impl IntoResponse {
    Json(health_check())
}

async fn create_session(
    State(state): State<AppState>,
    headers: HeaderMap,
    Json(payload): Json<SessionCreateRequest>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let storage_key =
        idempotency_storage_key("POST", "/trade-agent/v1/sessions", &account, &idempotency);

    if let Some(stored) = state
        .inner
        .lock()
        .await
        .idempotency
        .get(&storage_key)
        .cloned()
    {
        return Ok(stored.into_response());
    }

    if payload.authentication_key.trim().is_empty() {
        return Err(ApiError::bad_request(
            "authentication_key_empty",
            "authentication_key must not be empty",
        ));
    }

    let auth_hash = hash_secret(payload.auth_method, &payload.authentication_key, &account);

    let mut inner = state.inner.lock().await;
    let account_sessions = inner.sessions.entry(account.clone()).or_default();

    let previous_session_id = account_sessions.preempt_existing(&auth_hash);

    if let Some(previous) = previous_session_id {
        info!(account = %account, previous_session = %previous, "preempting previous session");
    }

    let mut session = SessionRecord::new(payload.auth_method, auth_hash.clone());

    if let Some(preapproval) = account_sessions.consume_preapproval(&auth_hash) {
        match session.promote(&auth_hash) {
            Ok(_) => match preapproval.approved_by.as_deref() {
                Some(operator) if !operator.is_empty() => info!(
                    account = %account,
                    session = %session.session_id,
                    operator,
                    "session authenticated via pre-approval",
                ),
                _ => info!(
                    account = %account,
                    session = %session.session_id,
                    "session authenticated via pre-approval",
                ),
            },
            Err(error) => warn!(
                account = %account,
                session = %session.session_id,
                %error,
                "failed to promote pre-approved session",
            ),
        }
    }

    let response_body = session.to_create_response(previous_session_id);

    let stored = StoredResponse::from_json(StatusCode::CREATED, &response_body)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    account_sessions.insert(session);
    inner.idempotency.insert(storage_key, stored.clone());

    info!(account = %account, session = %response_body.session_id, "session created");

    Ok(stored.into_response())
}

async fn delete_session(
    State(state): State<AppState>,
    headers: HeaderMap,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let token = bearer_token(&headers)?;
    let storage_key = idempotency_storage_key(
        "DELETE",
        "/trade-agent/v1/sessions/current",
        &account,
        &idempotency,
    );

    if let Some(stored) = state
        .inner
        .lock()
        .await
        .idempotency
        .get(&storage_key)
        .cloned()
    {
        return Ok(stored.into_response());
    }

    let mut inner = state.inner.lock().await;

    let (session_id, remove_account) = {
        let Some(account_sessions) = inner.sessions.get_mut(&account) else {
            return Err(ApiError::unauthorized(
                "session_missing",
                "no active session for the supplied account",
            ));
        };

        let Some(session) = account_sessions.get_mut_by_token(&token) else {
            return Err(ApiError::unauthorized(
                "invalid_session_token",
                "the provided session token is not valid for this account",
            ));
        };

        let session_id = session.session_id;
        account_sessions.remove_by_token(&token);
        (session_id, account_sessions.is_empty())
    };

    if remove_account {
        inner.sessions.remove(&account);
    }

    let stored = StoredResponse::empty(StatusCode::NO_CONTENT);
    inner.idempotency.insert(storage_key, stored.clone());

    info!(account = %account, session = %session_id, "session deleted");

    Ok(stored.into_response())
}

async fn ingest_inbox_events(
    State(state): State<AppState>,
    headers: HeaderMap,
    Json(payload): Json<InboxBatch>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let token = bearer_token(&headers)?;
    let storage_key = idempotency_storage_key(
        "POST",
        "/trade-agent/v1/sessions/current/inbox",
        &account,
        &idempotency,
    );

    if let Some(stored) = state
        .inner
        .lock()
        .await
        .idempotency
        .get(&storage_key)
        .cloned()
    {
        return Ok(stored.into_response());
    }

    let mut inner = state.inner.lock().await;
    let events = payload.events;

    let (accepted, pending) = {
        let Some(account_sessions) = inner.sessions.get_mut(&account) else {
            return Err(ApiError::unauthorized(
                "session_missing",
                "no active session for the supplied account",
            ));
        };

        let Some(session) = account_sessions.get_mut_by_token(&token) else {
            return Err(ApiError::unauthorized(
                "invalid_session_token",
                "the provided session token is not valid for this account",
            ));
        };

        if session.status == SessionStatus::Terminated {
            return Err(ApiError::forbidden(
                "session_terminated",
                "the session has been terminated and no longer accepts events",
            ));
        }

        let captured = session.capture_inbox(events);
        let accepted = captured.len();
        let pending = session.status.is_pending();
        debug!(account = %account, captured = accepted, "captured inbox events");
        (accepted, pending)
    };

    let response_body = InboxResponse {
        accepted,
        pending_session: pending,
    };

    let stored = StoredResponse::from_json(StatusCode::ACCEPTED, &response_body)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    inner.idempotency.insert(storage_key, stored.clone());

    Ok(stored.into_response())
}

async fn fetch_outbox_events(
    State(state): State<AppState>,
    headers: HeaderMap,
    Query(query): Query<OutboxQuery>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let token = bearer_token(&headers)?;

    let mut inner = state.inner.lock().await;

    let response = {
        let Some(account_sessions) = inner.sessions.get_mut(&account) else {
            return Err(ApiError::unauthorized(
                "session_missing",
                "no active session for the supplied account",
            ));
        };

        let Some(session) = account_sessions.get_mut_by_token(&token) else {
            return Err(ApiError::unauthorized(
                "invalid_session_token",
                "the provided session token is not valid for this account",
            ));
        };

        let cursor = query.cursor.unwrap_or_default();
        let events = session.events_after(cursor, query.limit, false);

        OutboxResponse {
            session_id: session.session_id,
            pending: session.status.is_pending(),
            events,
            retry_after_ms: 1_000,
        }
    };

    Ok((StatusCode::OK, Json(response)).into_response())
}

async fn acknowledge_outbox_event(
    State(state): State<AppState>,
    headers: HeaderMap,
    Path(event_id): Path<Uuid>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let token = bearer_token(&headers)?;
    let storage_key = idempotency_storage_key(
        "POST",
        &format!("/trade-agent/v1/sessions/current/outbox/{event_id}/ack"),
        &account,
        &idempotency,
    );

    if let Some(stored) = state
        .inner
        .lock()
        .await
        .idempotency
        .get(&storage_key)
        .cloned()
    {
        return Ok(stored.into_response());
    }

    let mut inner = state.inner.lock().await;

    let remaining = {
        let Some(account_sessions) = inner.sessions.get_mut(&account) else {
            return Err(ApiError::unauthorized(
                "session_missing",
                "no active session for the supplied account",
            ));
        };

        let Some(session) = account_sessions.get_mut_by_token(&token) else {
            return Err(ApiError::unauthorized(
                "invalid_session_token",
                "the provided session token is not valid for this account",
            ));
        };

        if !session.acknowledge_outbox(event_id) {
            return Err(ApiError::not_found(
                "event_not_found",
                "no outbox event with the supplied identifier",
            ));
        }

        session.outbox.len()
    };

    let response_body = AckResponse {
        acknowledged_event_id: event_id,
        remaining_outbox_depth: remaining,
    };

    let stored = StoredResponse::from_json(StatusCode::OK, &response_body)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    inner.idempotency.insert(storage_key, stored.clone());

    Ok(stored.into_response())
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionApprovalRequest {
    authentication_key: String,
}

async fn approve_session(
    State(state): State<AppState>,
    headers: HeaderMap,
    Path(session_id): Path<Uuid>,
    Json(payload): Json<SessionApprovalRequest>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let storage_key = idempotency_storage_key(
        "POST",
        &format!("/trade-agent/v1/sessions/{session_id}/approve"),
        &account,
        &idempotency,
    );

    if let Some(stored) = state.stored_response(&storage_key).await {
        return Ok(stored.into_response());
    }

    let promotion = state
        .promote_session_with_secret(&account, session_id, &payload.authentication_key)
        .await
        .map_err(|error| match error {
            AdminCommandError::SessionMissing => ApiError::unauthorized(
                "session_missing",
                "no active session for the supplied account",
            ),
            AdminCommandError::SessionMismatch => ApiError::conflict(
                "session_mismatch",
                "the supplied session id does not match the active session",
            ),
            AdminCommandError::AuthenticationFailed => ApiError::forbidden(
                "authentication_failed",
                "the provided authentication key is invalid",
            ),
            AdminCommandError::SessionTerminated => ApiError::conflict(
                "session_terminated",
                "the session has been terminated and cannot be approved",
            ),
            AdminCommandError::AuthenticationKeyEmpty => ApiError::bad_request(
                "authentication_key_empty",
                "authentication_key must not be empty",
            ),
        })?;

    let stored = StoredResponse::from_json(StatusCode::OK, &promotion)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    state.store_response(storage_key, stored.clone()).await;

    Ok(stored.into_response())
}

async fn queue_outbox_event(
    State(state): State<AppState>,
    headers: HeaderMap,
    Path(session_id): Path<Uuid>,
    Json(payload): Json<OutboxEventRequest>,
) -> Result<Response, ApiError> {
    let account = account_from_headers(&headers)?;
    let idempotency = idempotency_key(&headers)?;
    let storage_key = idempotency_storage_key(
        "POST",
        &format!("/trade-agent/v1/sessions/{session_id}/outbox"),
        &account,
        &idempotency,
    );

    if let Some(stored) = state
        .inner
        .lock()
        .await
        .idempotency
        .get(&storage_key)
        .cloned()
    {
        return Ok(stored.into_response());
    }

    if payload.event_type.trim().is_empty() {
        return Err(ApiError::bad_request(
            "event_type_empty",
            "event_type must not be empty",
        ));
    }

    let mut inner = state.inner.lock().await;
    let Some(account_sessions) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    let Some(session) = account_sessions.get_mut_by_session_id(&session_id) else {
        return Err(ApiError::conflict(
            "session_mismatch",
            "the supplied session id does not match the active session",
        ));
    };

    if session.status == SessionStatus::Terminated {
        return Err(ApiError::conflict(
            "session_terminated",
            "the session has been terminated and cannot accept outbox events",
        ));
    }

    let event = session.enqueue_outbox(payload);
    debug!(account = %account, event = %event.id, "queued outbox event");

    let response = OutboxEnqueueResponse {
        session_id,
        event_id: event.id,
        sequence: event.sequence,
        pending_session: session.status.is_pending(),
    };

    let stored = StoredResponse::from_json(StatusCode::ACCEPTED, &response)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    inner.idempotency.insert(storage_key, stored.clone());

    Ok(stored.into_response())
}

fn account_from_headers(headers: &HeaderMap) -> Result<String, ApiError> {
    header_value(headers, "X-TradeAgent-Account", "missing_account_header")
}

fn idempotency_key(headers: &HeaderMap) -> Result<String, ApiError> {
    header_value(headers, "Idempotency-Key", "missing_idempotency_key")
}

fn header_value(
    headers: &HeaderMap,
    name: &str,
    error_code: &'static str,
) -> Result<String, ApiError> {
    headers
        .get(name)
        .ok_or_else(|| ApiError::bad_request(error_code, format!("{name} header is required")))
        .and_then(|value| {
            value.to_str().map_err(|_| {
                ApiError::bad_request(
                    "invalid_header_encoding",
                    format!("{name} header must contain valid UTF-8"),
                )
            })
        })
        .and_then(|value| {
            let trimmed = value.trim();
            if trimmed.is_empty() {
                Err(ApiError::bad_request(
                    "empty_header_value",
                    format!("{name} header must not be empty"),
                ))
            } else {
                Ok(trimmed.to_string())
            }
        })
}

fn bearer_token(headers: &HeaderMap) -> Result<Uuid, ApiError> {
    let value = headers.get(header::AUTHORIZATION).ok_or_else(|| {
        ApiError::bad_request(
            "missing_authorization_header",
            "Authorization header with Bearer token is required",
        )
    })?;

    let value = value.to_str().map_err(|_| {
        ApiError::bad_request(
            "invalid_header_encoding",
            "Authorization header must be valid UTF-8",
        )
    })?;

    let Some(token) = value.strip_prefix("Bearer ") else {
        return Err(ApiError::bad_request(
            "invalid_authorization_scheme",
            "Authorization header must use the Bearer scheme",
        ));
    };

    Uuid::parse_str(token.trim()).map_err(|_| {
        ApiError::bad_request(
            "invalid_session_token",
            "Authorization token must be a valid UUID",
        )
    })
}

fn idempotency_storage_key(method: &str, path: &str, account: &str, key: &str) -> String {
    format!("{method}:{path}:{account}:{key}")
}

fn hash_secret(method: AuthMethod, secret: &str, account: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(method.storage_key().as_bytes());
    hasher.update(b":");
    hasher.update(account.as_bytes());
    hasher.update(b":");
    hasher.update(secret.as_bytes());
    format!("{:x}", hasher.finalize())
}

fn current_time() -> OffsetDateTime {
    OffsetDateTime::now_utc()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[tokio::test]
    async fn session_enqueue_and_ack_flow() {
        let account = "12345".to_string();
        let hash = hash_secret(AuthMethod::AccountSessionKey, "secret", &account);
        let mut session = SessionRecord::new(AuthMethod::AccountSessionKey, hash);

        assert_eq!(session.status, SessionStatus::Pending);
        assert!(session.outbox.is_empty());

        let event = session.enqueue_outbox(OutboxEventRequest {
            event_type: "OrderCommand".to_string(),
            payload: json!({"order_id": "abc"}),
            requires_ack: true,
        });

        assert_eq!(event.sequence, 1);
        assert_eq!(session.outbox.len(), 1);

        let pending_events = session.events_after(0, None, true);
        assert_eq!(pending_events.len(), 1);

        assert!(session.acknowledge_outbox(event.id));
        assert!(session.outbox.is_empty());
    }

    #[test]
    fn hashing_is_deterministic() {
        let hash1 = hash_secret(AuthMethod::AccountSessionKey, "secret", "account");
        let hash2 = hash_secret(AuthMethod::AccountSessionKey, "secret", "account");
        let hash3 = hash_secret(AuthMethod::AccountSessionKey, "secret", "other");

        assert_eq!(hash1, hash2);
        assert_ne!(hash1, hash3);
    }

    #[test]
    fn health_check_returns_ok_status() {
        let status = health_check();

        assert_eq!(status, HealthStatus::ok("gateway"));
        assert!(status.healthy);
        assert_eq!(status.message, "ok");
    }

    #[tokio::test]
    async fn service_bus_promotion_authenticates_session() {
        let state = AppState::default();
        let account = "acct-service".to_string();
        let fingerprint = hash_secret(AuthMethod::AccountSessionKey, "secret", &account);
        let session_id;

        {
            let mut inner = state.inner.lock().await;
            let account_sessions = inner.sessions.entry(account.clone()).or_default();
            let session = SessionRecord::new(AuthMethod::AccountSessionKey, fingerprint.clone());
            session_id = session.session_id;
            account_sessions.insert(session);
        }

        let promotion = state
            .promote_session_with_fingerprint(&account, session_id, &fingerprint, Some("ops"))
            .await
            .expect("promotion should succeed");

        assert_eq!(promotion.session_id, session_id);
        assert_eq!(promotion.status, SessionStatus::Authenticated);
        assert!(!promotion.pending);

        let mut inner = state.inner.lock().await;
        let account_sessions = inner.sessions.get_mut(&account).expect("account missing");
        let session = account_sessions
            .get_mut_by_session_id(&session_id)
            .expect("session missing");
        assert_eq!(session.status, SessionStatus::Authenticated);
        assert_eq!(session.outbox.len(), 1);
        assert_eq!(session.outbox[0].event_type, "InitAck");
    }

    #[tokio::test]
    async fn reject_session_marks_session_terminated() {
        let state = AppState::default();
        let account = "acct-reject".to_string();
        let fingerprint = hash_secret(AuthMethod::AccountSessionKey, "secret", &account);
        let session_id;

        {
            let mut inner = state.inner.lock().await;
            let account_sessions = inner.sessions.entry(account.clone()).or_default();
            let session = SessionRecord::new(AuthMethod::AccountSessionKey, fingerprint.clone());
            session_id = session.session_id;
            account_sessions.insert(session);
        }

        let rejection = state
            .reject_session(
                &account,
                session_id,
                &fingerprint,
                Some("not approved".to_string()),
                Some("operator".to_string()),
            )
            .await
            .expect("rejection should succeed");

        assert_eq!(rejection.session_id, session_id);
        assert_eq!(rejection.status, SessionStatus::Terminated);
        assert_eq!(rejection.reason.as_deref(), Some("not approved"));

        let mut inner = state.inner.lock().await;
        let account_sessions = inner.sessions.get_mut(&account).expect("account missing");
        let session = account_sessions
            .get_mut_by_session_id(&session_id)
            .expect("session missing");
        assert_eq!(session.status, SessionStatus::Terminated);
        let shutdown = session.outbox.last().expect("missing shutdown");
        assert_eq!(shutdown.event_type, "ShutdownNotice");
        assert_eq!(
            shutdown
                .payload
                .get("reason")
                .and_then(|value| value.as_str()),
            Some("session_rejected")
        );
        assert_eq!(
            shutdown
                .payload
                .get("rejectedBy")
                .and_then(|value| value.as_str()),
            Some("operator")
        );
    }
}
