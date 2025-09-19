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

    let response = app
        .clone()
        .oneshot(stale_token_request)
        .await
        .expect("router error");
    assert_eq!(response.status(), StatusCode::UNAUTHORIZED);
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
struct OutboxResponsePayload {
    pending: bool,
    events: Vec<OutboxEventPayload>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxEventPayload {
    id: Uuid,
    sequence: u64,
    event_type: String,
    requires_ack: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct AckResponsePayload {
    acknowledged_event_id: Uuid,
    remaining_outbox_depth: usize,
}
