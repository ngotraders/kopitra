use std::fmt::Debug;

use axum::{
    body::Body,
    http::{self, Request, StatusCode, header},
    response::Response,
};
use gateway::{AppState, SessionStatus, router};
use http_body_util::BodyExt;
use serde::{Deserialize, de::DeserializeOwned};
use serde_json::json;
use tower::ServiceExt;
use uuid::Uuid;

#[tokio::test]
async fn session_auth_flow_emits_init_ack() {
    let app = router(AppState::default());
    let account = "acct-001";
    let auth_key = "shared-secret";
    let create_idempotency = Uuid::new_v4().to_string();

    let create_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", create_idempotency.as_str())
        .body(Body::from(
            json!({
                "authMethod": "account_session_key",
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build create session request");

    let (status, created) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(create_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);
    assert_eq!(created.status, SessionStatus::Pending);
    assert!(created.pending);
    assert!(created.previous_session_terminated.is_none());

    let session_id = created.session_id;
    let session_token = created.session_token;

    let outbox_before_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, outbox_before) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_before_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(outbox_before.pending);
    assert!(outbox_before.events.is_empty());

    let approve_idempotency = Uuid::new_v4().to_string();
    let approve_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!("/trade-agent/v1/sessions/{session_id}/approve"))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", approve_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build approve request");

    let (status, approved) = json_response::<SessionPromotionResponsePayload>(
        app.clone()
            .oneshot(approve_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(approved.session_id, session_id);
    assert_eq!(approved.status, SessionStatus::Authenticated);
    assert!(!approved.pending);

    let outbox_after_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, outbox_after) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_after_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(!outbox_after.pending);
    assert_eq!(outbox_after.events.len(), 1);

    let init_ack = &outbox_after.events[0];
    assert_eq!(init_ack.sequence, 1);
    assert_eq!(init_ack.event_type, "InitAck");
    assert!(init_ack.requires_ack);

    let ack_idempotency = Uuid::new_v4().to_string();
    let ack_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/current/outbox/{}/ack",
            init_ack.id
        ))
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build ack request");

    let (status, acked) = json_response::<AckResponsePayload>(
        app.clone()
            .oneshot(ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(acked.acknowledged_event_id, init_ack.id);
    assert_eq!(acked.remaining_outbox_depth, 0);

    let outbox_after_ack_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, outbox_final) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_after_ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(outbox_final.events.is_empty());
    assert!(!outbox_final.pending);
}

#[tokio::test]
async fn pending_session_ingests_events_and_remains_pending() {
    let app = router(AppState::default());
    let account = "acct-003";
    let auth_key = "pending-secret";

    let create_idempotency = Uuid::new_v4().to_string();
    let create_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", create_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build create session request");

    let (status, created) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(create_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);
    assert!(created.pending);

    let session_token = created.session_token;

    let inbox_idempotency = Uuid::new_v4().to_string();
    let inbox_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", inbox_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "InitRequest",
                        "payload": {
                            "accountLogin": "1001",
                            "broker": "ExampleBroker",
                        }
                    },
                    {
                        "eventType": "StatusHeartbeat",
                        "payload": {
                            "state": "pending",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build inbox request");

    let inbox_response = app
        .clone()
        .oneshot(inbox_request)
        .await
        .expect("router error");
    let inbox_status = inbox_response.status();
    let inbox_body = inbox_response
        .into_body()
        .collect()
        .await
        .expect("body collection failed")
        .to_bytes();
    assert_eq!(
        inbox_status,
        StatusCode::ACCEPTED,
        "unexpected inbox status: {}",
        String::from_utf8_lossy(&inbox_body)
    );
    let inbox: InboxResponsePayload =
        serde_json::from_slice(&inbox_body).expect("failed to deserialize inbox response");
    assert_eq!(inbox.accepted, 2);
    assert!(inbox.pending_session);

    let outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(outbox.pending);
    assert!(outbox.events.is_empty());
}

#[tokio::test]
async fn session_creation_is_idempotent_and_preempts_previous() {
    let app = router(AppState::default());
    let account = "acct-002";
    let auth_key = "rotating-secret";

    let idempotency_key = Uuid::new_v4().to_string();
    let create_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", idempotency_key.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build create session request");

    let (status, first) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(create_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);

    let replay_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", idempotency_key.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build replay request");

    let (status, replayed) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(replay_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);
    assert_eq!(replayed.session_id, first.session_id);
    assert_eq!(replayed.session_token, first.session_token);

    let replacement_idempotency = Uuid::new_v4().to_string();
    let replacement_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", replacement_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build replacement request");

    let (status, replacement) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(replacement_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);
    assert_ne!(replacement.session_id, first.session_id);
    assert_eq!(
        replacement.previous_session_terminated,
        Some(first.session_id)
    );

    let stale_token_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(
            header::AUTHORIZATION,
            format!("Bearer {}", first.session_token),
        )
        .body(Body::empty())
        .expect("failed to build stale token request");

    let (status, stale_outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(stale_token_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(!stale_outbox.pending);
    assert_eq!(stale_outbox.events.len(), 1);
    let shutdown = &stale_outbox.events[0];
    assert_eq!(shutdown.event_type, "ShutdownNotice");
    assert!(shutdown.requires_ack);
    assert_eq!(
        shutdown
            .payload
            .get("reason")
            .and_then(|value| value.as_str()),
        Some("session_preempted")
    );
}

#[tokio::test]
async fn terminated_session_rejects_inbox_submission() {
    let app = router(AppState::default());
    let account = "acct-004";
    let auth_key = "one-time-secret";

    let create_idempotency = Uuid::new_v4().to_string();
    let create_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", create_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build create session request");

    let (status, created) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(create_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);

    let session_token = created.session_token;

    let approve_idempotency = Uuid::new_v4().to_string();
    let approve_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/approve",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", approve_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build approve request");

    let (status, _) = json_response::<SessionPromotionResponsePayload>(
        app.clone()
            .oneshot(approve_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);

    let replacement_idempotency = Uuid::new_v4().to_string();
    let replacement_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", replacement_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build replacement request");

    let (status, _) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(replacement_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);

    let inbox_idempotency = Uuid::new_v4().to_string();
    let inbox_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", inbox_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [{
                    "eventType": "StatusHeartbeat",
                    "payload": {"state": "terminated"}
                }]
            })
            .to_string(),
        ))
        .expect("failed to build inbox request");

    let response = app
        .clone()
        .oneshot(inbox_request)
        .await
        .expect("router error");
    assert_eq!(response.status(), StatusCode::FORBIDDEN);
    let body = response
        .into_body()
        .collect()
        .await
        .expect("body collection failed")
        .to_bytes();
    let error: ErrorResponsePayload =
        serde_json::from_slice(&body).expect("failed to deserialize error response");
    assert_eq!(error.code, "session_terminated");
    assert!(
        error.message.contains("terminated"),
        "unexpected error message: {}",
        error.message
    );
}

#[tokio::test]
async fn pre_shared_key_sessions_authenticate_successfully() {
    let app = router(AppState::default());
    let account = "acct-psk";
    let auth_key = "shared-pre-key";

    let create_idempotency = Uuid::new_v4().to_string();
    let create_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", create_idempotency.as_str())
        .body(Body::from(
            json!({
                "authMethod": "pre_shared_key",
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build create request");

    let (status, created) = json_response::<SessionCreateResponsePayload>(
        app.clone()
            .oneshot(create_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::CREATED);

    let approve_idempotency = Uuid::new_v4().to_string();
    let approve_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/approve",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", approve_idempotency.as_str())
        .body(Body::from(
            json!({
                "authenticationKey": auth_key,
            })
            .to_string(),
        ))
        .expect("failed to build approve request");

    let (status, approved) = json_response::<SessionPromotionResponsePayload>(
        app.clone()
            .oneshot(approve_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(approved.session_id, created.session_id);
    assert_eq!(approved.status, SessionStatus::Authenticated);

    let outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(
            header::AUTHORIZATION,
            format!("Bearer {}", created.session_token),
        )
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(outbox.session_id, created.session_id);
    assert_eq!(outbox.events.len(), 1);
    assert_eq!(outbox.events[0].event_type, "InitAck");
}

async fn json_response<T>(response: Response) -> (StatusCode, T)
where
    T: DeserializeOwned + Debug,
{
    let status = response.status();
    let bytes = response
        .into_body()
        .collect()
        .await
        .expect("body collection failed")
        .to_bytes();
    let payload = serde_json::from_slice(&bytes).expect("failed to deserialize response");
    (status, payload)
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateResponsePayload {
    session_id: Uuid,
    session_token: Uuid,
    status: SessionStatus,
    pending: bool,
    previous_session_terminated: Option<Uuid>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionPromotionResponsePayload {
    session_id: Uuid,
    status: SessionStatus,
    pending: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxResponsePayload {
    accepted: usize,
    pending_session: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxResponsePayload {
    session_id: Uuid,
    pending: bool,
    events: Vec<OutboxEventPayload>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxEventPayload {
    id: Uuid,
    sequence: u64,
    event_type: String,
    payload: serde_json::Value,
    requires_ack: bool,
}

#[derive(Debug, Deserialize)]
struct ErrorResponsePayload {
    code: String,
    message: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct AckResponsePayload {
    acknowledged_event_id: Uuid,
    remaining_outbox_depth: usize,
}
