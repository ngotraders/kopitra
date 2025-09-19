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
use serde_json::{Value, json};
use sha2::{Digest, Sha256};
use thiserror::Error;
use time::OffsetDateTime;
use tokio::sync::Mutex;
use tracing::{debug, info};
use uuid::Uuid;

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
    sessions: HashMap<String, SessionRecord>,
    idempotency: HashMap<String, StoredResponse>,
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
}

impl Default for AuthMethod {
    fn default() -> Self {
        AuthMethod::AccountSessionKey
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

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct SessionPromotionResponse {
    session_id: Uuid,
    status: SessionStatus,
    pending: bool,
    message: String,
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

    fn mark_authenticated(&mut self) {
        self.status = SessionStatus::Authenticated;
        self.updated_at = current_time();
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

    fn capture_inbox(&mut self, batch: Vec<InboxEvent>) -> Vec<InboundEventRecord> {
        let mut captured = Vec::with_capacity(batch.len());
        for event in batch {
            let received_at = current_time();
            let event_type_lower = event.event_type.to_ascii_lowercase();
            if event_type_lower.contains("heartbeat") {
                self.last_heartbeat_at = Some(received_at);
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

    let auth_hash = hash_secret(&payload.authentication_key, &account);

    let mut inner = state.inner.lock().await;

    let previous_session = inner.sessions.remove(&account);
    let previous_session_id = previous_session.as_ref().map(|session| session.session_id);

    if let Some(previous) = previous_session {
        info!(
            account = %account,
            previous_session = %previous.session_id,
            "preempting previous session"
        );
    }

    let session = SessionRecord::new(payload.auth_method, auth_hash);
    let response_body = session.to_create_response(previous_session_id);

    let stored = StoredResponse::from_json(StatusCode::CREATED, &response_body)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    inner.sessions.insert(account.clone(), session);
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
    let Some(session) = inner.sessions.get(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_token != token {
        return Err(ApiError::unauthorized(
            "invalid_session_token",
            "the provided session token is not valid for this account",
        ));
    }

    inner.sessions.remove(&account);
    let stored = StoredResponse::empty(StatusCode::NO_CONTENT);
    inner.idempotency.insert(storage_key, stored.clone());

    info!(account = %account, "session deleted");

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
    let Some(session) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_token != token {
        return Err(ApiError::unauthorized(
            "invalid_session_token",
            "the provided session token is not valid for this account",
        ));
    }

    let captured = session.capture_inbox(payload.events);
    debug!(account = %account, captured = captured.len(), "captured inbox events");

    let response_body = InboxResponse {
        accepted: captured.len(),
        pending_session: session.status.is_pending(),
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
    let Some(session) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_token != token {
        return Err(ApiError::unauthorized(
            "invalid_session_token",
            "the provided session token is not valid for this account",
        ));
    }

    let cursor = query.cursor.unwrap_or_default();
    let events = session.events_after(cursor, query.limit, false);

    let response = OutboxResponse {
        session_id: session.session_id,
        pending: session.status.is_pending(),
        events,
        retry_after_ms: 1_000,
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
    let Some(session) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_token != token {
        return Err(ApiError::unauthorized(
            "invalid_session_token",
            "the provided session token is not valid for this account",
        ));
    }

    if !session.acknowledge_outbox(event_id) {
        return Err(ApiError::not_found(
            "event_not_found",
            "no outbox event with the supplied identifier",
        ));
    }

    let response_body = AckResponse {
        acknowledged_event_id: event_id,
        remaining_outbox_depth: session.outbox.len(),
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
    let Some(session) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_id != session_id {
        return Err(ApiError::conflict(
            "session_mismatch",
            "the supplied session id does not match the active session",
        ));
    }

    if payload.authentication_key.trim().is_empty() {
        return Err(ApiError::bad_request(
            "authentication_key_empty",
            "authentication_key must not be empty",
        ));
    }

    let auth_hash = hash_secret(&payload.authentication_key, &account);

    if !session.verify_secret(&auth_hash) {
        return Err(ApiError::forbidden(
            "authentication_failed",
            "the provided authentication key is invalid",
        ));
    }

    if session.status == SessionStatus::Authenticated {
        let response = SessionPromotionResponse {
            session_id,
            status: session.status,
            pending: false,
            message: "session already authenticated".to_string(),
        };
        let stored = StoredResponse::from_json(StatusCode::OK, &response)
            .map_err(|error| ApiError::internal(error.to_string()))?;
        inner.idempotency.insert(storage_key, stored.clone());
        return Ok(stored.into_response());
    }

    session.mark_authenticated();
    session.enqueue_init_ack();

    info!(account = %account, session = %session_id, "session authenticated");

    let response = SessionPromotionResponse {
        session_id,
        status: session.status,
        pending: false,
        message: "session authenticated".to_string(),
    };

    let stored = StoredResponse::from_json(StatusCode::OK, &response)
        .map_err(|error| ApiError::internal(error.to_string()))?;

    inner.idempotency.insert(storage_key, stored.clone());

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
    let Some(session) = inner.sessions.get_mut(&account) else {
        return Err(ApiError::unauthorized(
            "session_missing",
            "no active session for the supplied account",
        ));
    };

    if session.session_id != session_id {
        return Err(ApiError::conflict(
            "session_mismatch",
            "the supplied session id does not match the active session",
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

fn hash_secret(secret: &str, account: &str) -> String {
    let mut hasher = Sha256::new();
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
        let hash = hash_secret("secret", &account);
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
        let hash1 = hash_secret("secret", "account");
        let hash2 = hash_secret("secret", "account");
        let hash3 = hash_secret("secret", "other");

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
}
