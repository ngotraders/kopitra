use std::fmt::Debug;

use axum::{
    body::Body,
    http::{self, Request, StatusCode, header},
    response::Response,
};
use gateway::{
    AdminApprovalCommand, AdminCommand, AdminCommandOutcome, AppState, AuthMethod, SessionStatus,
    router,
};
use http_body_util::BodyExt;
use serde::{Deserialize, de::DeserializeOwned};
use serde_json::json;
use sha2::{Digest, Sha256};
use tower::ServiceExt;
use uuid::Uuid;

#[tokio::test]
async fn session_auth_flow_emits_init_ack() {
    let state = AppState::default();
    let app = router(state.clone());
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

    approve_session_via_service_bus(&state, account, session_id, created.auth_method, auth_key)
        .await;

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
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "OutboxAck",
                        "payload": {
                            "eventId": init_ack.id,
                            "sequence": init_ack.sequence,
                            "status": "received",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build ack request");

    let (status, ack_response) = json_response::<InboxResponsePayload>(
        app.clone()
            .oneshot(ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(ack_response.accepted, 1);
    assert!(!ack_response.pending_session);

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
async fn preapproved_session_is_immediately_authenticated() {
    let state = AppState::default();
    let account = "acct-preapproved";
    let auth_key = "preapproved-secret";

    state
        .preapprove_session_key(
            account,
            AuthMethod::AccountSessionKey,
            auth_key,
            Some("ops-console".to_string()),
            None,
        )
        .await
        .expect("pre-approval registration should succeed");

    let app = router(state.clone());
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
    assert_eq!(created.status, SessionStatus::Authenticated);
    assert!(!created.pending);
    assert_eq!(created.auth_method, AuthMethod::AccountSessionKey);
    assert!(created.previous_session_terminated.is_none());

    let session_token = created.session_token;

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
    assert_eq!(outbox.session_id, created.session_id);
    assert!(!outbox.pending);
    assert_eq!(outbox.events.len(), 1);

    let init_ack = &outbox.events[0];
    assert_eq!(init_ack.sequence, 1);
    assert_eq!(init_ack.event_type, "InitAck");
    assert!(init_ack.requires_ack);
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
async fn management_can_fetch_inbox_logs_with_filters() {
    let state = AppState::default();
    let app = router(state.clone());
    let account = "acct-telemetry";
    let auth_key = "telemetry-secret";

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

    let session_id = created.session_id;
    let session_token = created.session_token;

    let telemetry_idempotency = Uuid::new_v4().to_string();
    let inbox_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", telemetry_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "EaSnapshot",
                        "occurredAt": "2024-06-01T08:00:00Z",
                        "payload": {
                            "equity": 105432.10,
                            "balance": 103876.55,
                            "currencyPairs": ["EURUSD", "USDJPY", "GBPUSD"],
                        }
                    },
                    {
                        "eventType": "TradeUpdate",
                        "occurredAt": "2024-06-02T09:30:00Z",
                        "payload": {
                            "ticket": "12345",
                            "instrument": "EURUSD",
                            "side": "buy",
                            "volume": 0.75,
                        }
                    },
                    {
                        "eventType": "BalanceUpdate",
                        "occurredAt": "2024-06-03T10:15:00Z",
                        "payload": {
                            "balance": 104100.25,
                            "reason": "deposit",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build telemetry inbox request");

    let (status, inbox_response) = json_response::<InboxResponsePayload>(
        app.clone()
            .oneshot(inbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(inbox_response.accepted, 3);

    let logs_request = Request::builder()
        .method(http::Method::GET)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/inbox/logs?limit=10",
            session_id
        ))
        .header("X-TradeAgent-Account", account)
        .body(Body::empty())
        .expect("failed to build inbox logs request");

    let (status, logs) = json_response::<InboxLogResponsePayload>(
        app.clone()
            .oneshot(logs_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(logs.session_id, session_id);
    assert!(logs.pending_session);
    assert_eq!(logs.events.len(), 3);
    assert_eq!(logs.next_cursor, logs.events.last().unwrap().sequence);
    assert_eq!(logs.events[0].event_type, "EaSnapshot");
    assert!(logs.events[0].occurred_at.as_deref().is_some());
    assert!(!logs.events[0].received_at.is_empty());
    assert_eq!(
        logs.events[0]
            .payload
            .get("currencyPairs")
            .and_then(|value| value.as_array())
            .map(|pairs| pairs.len()),
        Some(3)
    );
    assert_ne!(logs.events[0].id, logs.events[1].id);

    let first_sequence = logs.events.first().unwrap().sequence;

    let cursor_request = Request::builder()
        .method(http::Method::GET)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/inbox/logs?cursor={}&limit=10",
            session_id, first_sequence
        ))
        .header("X-TradeAgent-Account", account)
        .body(Body::empty())
        .expect("failed to build cursor logs request");

    let (status, cursor_logs) = json_response::<InboxLogResponsePayload>(
        app.clone()
            .oneshot(cursor_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(cursor_logs.events.len(), 2);

    let event_type_request = Request::builder()
        .method(http::Method::GET)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/inbox/logs?eventType=tradeupdate",
            session_id
        ))
        .header("X-TradeAgent-Account", account)
        .body(Body::empty())
        .expect("failed to build event type filter request");

    let (status, trade_logs) = json_response::<InboxLogResponsePayload>(
        app.clone()
            .oneshot(event_type_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(trade_logs.events.len(), 1);
    assert_eq!(trade_logs.events[0].event_type, "TradeUpdate");

    let occurred_after = "2024-06-02T00:00:00Z".replace(':', "%3A");
    let after_request = Request::builder()
        .method(http::Method::GET)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/inbox/logs?occurredAfter={}",
            session_id, occurred_after
        ))
        .header("X-TradeAgent-Account", account)
        .body(Body::empty())
        .expect("failed to build occurredAfter request");

    let (status, after_logs) = json_response::<InboxLogResponsePayload>(
        app.clone()
            .oneshot(after_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(after_logs.events.len(), 2);

    let occurred_before = "2024-06-02T12:00:00Z".replace(':', "%3A");
    let before_request = Request::builder()
        .method(http::Method::GET)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/inbox/logs?occurredBefore={}",
            session_id, occurred_before
        ))
        .header("X-TradeAgent-Account", account)
        .body(Body::empty())
        .expect("failed to build occurredBefore request");

    let (status, before_logs) = json_response::<InboxLogResponsePayload>(
        app.clone()
            .oneshot(before_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(before_logs.events.len(), 2);
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
    let state = AppState::default();
    let app = router(state.clone());
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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

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
    let state = AppState::default();
    let app = router(state.clone());
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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

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

#[tokio::test]
async fn admin_can_queue_market_order_for_authenticated_session() {
    let state = AppState::default();
    let app = router(state.clone());
    let account = "acct-orders-001";
    let auth_key = "orders-secret";

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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

    let session_token = created.session_token;

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
    assert_eq!(outbox.events.len(), 1);
    let init_ack = &outbox.events[0];

    let ack_idempotency = Uuid::new_v4().to_string();
    let ack_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "OutboxAck",
                        "payload": {
                            "eventId": init_ack.id,
                            "sequence": init_ack.sequence,
                            "status": "received",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build ack request");

    let (status, ack) = json_response::<InboxResponsePayload>(
        app.clone()
            .oneshot(ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(ack.accepted, 1);

    let order_idempotency = Uuid::new_v4().to_string();
    let order_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/orders",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", order_idempotency.as_str())
        .body(Body::from(
            json!({
                "commandType": "open",
                "instrument": "USDJPY",
                "orderType": "market",
                "side": "buy",
                "volume": 1.25,
                "timeInForce": "gtc",
                "takeProfit": 151.25,
                "stopLoss": 149.75,
                "clientOrderId": "ops-1234",
                "metadata": {
                    "source": "ops-console",
                    "reason": "manual",
                }
            })
            .to_string(),
        ))
        .expect("failed to build order request");

    let (status, order_response) = json_response::<TradeOrderResponsePayload>(
        app.clone()
            .oneshot(order_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(order_response.session_id, created.session_id);
    assert_eq!(order_response.command_type, "open");
    assert_eq!(order_response.instrument, "USDJPY");
    assert_eq!(order_response.order_type.as_deref(), Some("market"));
    assert_eq!(order_response.side.as_deref(), Some("buy"));
    assert_eq!(order_response.volume, Some(1.25));
    assert!(!order_response.pending_session);
    assert_ne!(order_response.command_id, Uuid::nil());

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
    assert_eq!(outbox_after.events.len(), 1);
    let event = &outbox_after.events[0];
    assert_eq!(event.id, order_response.event_id);
    assert_eq!(event.sequence, order_response.sequence);
    assert_eq!(event.event_type, "OrderCommand");

    let payload = &event.payload;
    assert_eq!(
        payload.get("commandType").and_then(|v| v.as_str()),
        Some("open")
    );
    assert_eq!(
        payload.get("instrument").and_then(|v| v.as_str()),
        Some("USDJPY")
    );
    assert_eq!(
        payload.get("orderType").and_then(|v| v.as_str()),
        Some("market")
    );
    assert_eq!(payload.get("side").and_then(|v| v.as_str()), Some("buy"));
    assert_eq!(payload.get("volume").and_then(|v| v.as_f64()), Some(1.25));
    assert_eq!(
        payload.get("timeInForce").and_then(|v| v.as_str()),
        Some("gtc")
    );
    assert_eq!(
        payload.get("clientOrderId").and_then(|v| v.as_str()),
        Some("ops-1234")
    );
    assert_eq!(
        payload
            .get("metadata")
            .and_then(|value| value.get("source"))
            .and_then(|value| value.as_str()),
        Some("ops-console"),
    );

    let trade_ack_idempotency = Uuid::new_v4().to_string();
    let trade_ack_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", trade_ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "OutboxAck",
                        "payload": {
                            "eventId": event.id,
                            "sequence": event.sequence,
                            "status": "received",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build trade ack request");

    let (status, trade_ack) = json_response::<InboxResponsePayload>(
        app.clone()
            .oneshot(trade_ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(trade_ack.accepted, 1);

    let final_outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build final outbox request");

    let (status, final_outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(final_outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(final_outbox.events.is_empty());
}

#[tokio::test]
async fn admin_can_queue_close_command_with_defaults() {
    let state = AppState::default();
    let app = router(state.clone());
    let account = "acct-orders-002";
    let auth_key = "orders-secret";

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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

    let session_token = created.session_token;

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
    assert_eq!(outbox.events.len(), 1);
    let init_ack = &outbox.events[0];

    let ack_idempotency = Uuid::new_v4().to_string();
    let ack_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "OutboxAck",
                        "payload": {
                            "eventId": init_ack.id,
                            "sequence": init_ack.sequence,
                            "status": "received",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build ack request");

    let ack_response = app
        .clone()
        .oneshot(ack_request)
        .await
        .expect("router error");
    assert_eq!(ack_response.status(), StatusCode::ACCEPTED);

    let close_idempotency = Uuid::new_v4().to_string();
    let close_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/orders",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", close_idempotency.as_str())
        .body(Body::from(
            json!({
                "commandType": "close",
                "instrument": "GBPUSD",
                "positionId": "ticket-100",
                "volume": 0.75
            })
            .to_string(),
        ))
        .expect("failed to build close request");

    let (status, close_response) = json_response::<TradeOrderResponsePayload>(
        app.clone()
            .oneshot(close_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(close_response.session_id, created.session_id);
    assert_eq!(close_response.command_type, "close");
    assert_eq!(close_response.order_type.as_deref(), Some("market"));
    assert_eq!(close_response.position_id.as_deref(), Some("ticket-100"));
    assert_eq!(close_response.volume, Some(0.75));
    assert!(!close_response.pending_session);
    assert_ne!(close_response.command_id, Uuid::nil());

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
    assert_eq!(outbox_after.events.len(), 1);
    let event = &outbox_after.events[0];
    assert_eq!(event.event_type, "OrderCommand");
    assert_eq!(
        event
            .payload
            .get("orderType")
            .and_then(|value| value.as_str()),
        Some("market"),
    );
    assert_eq!(
        event
            .payload
            .get("positionId")
            .and_then(|value| value.as_str()),
        Some("ticket-100"),
    );
    assert_eq!(
        event.payload.get("volume").and_then(|v| v.as_f64()),
        Some(0.75)
    );
    assert_eq!(
        event
            .payload
            .get("commandType")
            .and_then(|value| value.as_str()),
        Some("close"),
    );
}

#[tokio::test]
async fn close_command_requires_position_id() {
    let state = AppState::default();
    let app = router(state.clone());
    let account = "acct-orders-003";
    let auth_key = "orders-secret";

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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

    let session_token = created.session_token;

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
    assert_eq!(outbox.events.len(), 1);
    let init_ack = &outbox.events[0];

    let ack_idempotency = Uuid::new_v4().to_string();
    let ack_request = Request::builder()
        .method(http::Method::POST)
        .uri("/trade-agent/v1/sessions/current/inbox")
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::from(
            json!({
                "events": [
                    {
                        "eventType": "OutboxAck",
                        "payload": {
                            "eventId": init_ack.id,
                            "sequence": init_ack.sequence,
                            "status": "received",
                        }
                    }
                ]
            })
            .to_string(),
        ))
        .expect("failed to build ack request");

    let ack_response = app
        .clone()
        .oneshot(ack_request)
        .await
        .expect("router error");
    assert_eq!(ack_response.status(), StatusCode::ACCEPTED);

    let close_idempotency = Uuid::new_v4().to_string();
    let invalid_close_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/orders",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", close_idempotency.as_str())
        .body(Body::from(
            json!({
                "commandType": "close",
                "instrument": "EURUSD",
                "volume": 0.5
            })
            .to_string(),
        ))
        .expect("failed to build invalid close request");

    let (status, error) = json_response::<ErrorResponsePayload>(
        app.clone()
            .oneshot(invalid_close_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::BAD_REQUEST);
    assert_eq!(error.code, "position_id_required");
}

#[tokio::test]
async fn management_can_queue_copy_trade_updates() {
    let state = AppState::default();
    let app = router(state.clone());
    let account = "acct-copy-management";
    let auth_key = "copy-management-secret";

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

    approve_session_via_service_bus(
        &state,
        account,
        created.session_id,
        created.auth_method,
        auth_key,
    )
    .await;

    let session_token = created.session_token;

    let outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build outbox request");

    let (status, initial_outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(initial_outbox.events.len(), 1);
    let init_ack = &initial_outbox.events[0];

    let init_ack_idempotency = Uuid::new_v4().to_string();
    let init_ack_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/current/outbox/{}/ack",
            init_ack.id
        ))
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", init_ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build init ack request");

    let (status, ack_response) = json_response::<OutboxAckResponsePayload>(
        app.clone()
            .oneshot(init_ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(ack_response.acknowledged_event_id, init_ack.id);
    assert_eq!(ack_response.remaining_outbox_depth, 0);

    let empty_outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build empty outbox request");

    let (status, empty_outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(empty_outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(empty_outbox.events.is_empty());

    let copy_idempotency = Uuid::new_v4().to_string();
    let copy_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/{}/outbox",
            created.session_id
        ))
        .header(header::CONTENT_TYPE, "application/json")
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", copy_idempotency.as_str())
        .body(Body::from(
            json!({
                "eventType": "CopyTradeConfig",
                "payload": {
                    "groupId": "copy-group-1",
                    "action": "update",
                    "parameters": {
                        "maxDeviationPips": 2.5,
                        "symbolFilter": ["EURUSD", "USDJPY"],
                        "riskMultiplier": 1.2,
                    }
                }
            })
            .to_string(),
        ))
        .expect("failed to build copy trade request");

    let (status, enqueue_response) = json_response::<OutboxEnqueueResponsePayload>(
        app.clone()
            .oneshot(copy_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::ACCEPTED);
    assert_eq!(enqueue_response.session_id, created.session_id);
    assert!(!enqueue_response.pending_session);

    let copy_outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build copy outbox request");

    let (status, copy_outbox) = json_response::<OutboxResponsePayload>(
        app.clone()
            .oneshot(copy_outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(copy_outbox.events.len(), 1);
    let copy_event = &copy_outbox.events[0];
    assert_eq!(copy_event.event_type, "CopyTradeConfig");
    assert!(copy_event.requires_ack);
    assert_eq!(enqueue_response.event_id, copy_event.id);
    assert_eq!(enqueue_response.sequence, copy_event.sequence);
    assert_eq!(
        copy_event
            .payload
            .get("parameters")
            .and_then(|value| value.get("riskMultiplier"))
            .and_then(|value| value.as_f64()),
        Some(1.2)
    );

    let copy_ack_idempotency = Uuid::new_v4().to_string();
    let copy_ack_request = Request::builder()
        .method(http::Method::POST)
        .uri(format!(
            "/trade-agent/v1/sessions/current/outbox/{}/ack",
            copy_event.id
        ))
        .header("X-TradeAgent-Account", account)
        .header("Idempotency-Key", copy_ack_idempotency.as_str())
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build copy ack request");

    let (status, copy_ack) = json_response::<OutboxAckResponsePayload>(
        app.clone()
            .oneshot(copy_ack_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert_eq!(copy_ack.acknowledged_event_id, copy_event.id);
    assert_eq!(copy_ack.remaining_outbox_depth, 0);

    let final_outbox_request = Request::builder()
        .method(http::Method::GET)
        .uri("/trade-agent/v1/sessions/current/outbox")
        .header("X-TradeAgent-Account", account)
        .header(header::AUTHORIZATION, format!("Bearer {session_token}"))
        .body(Body::empty())
        .expect("failed to build final outbox request");

    let (status, final_outbox) = json_response::<OutboxResponsePayload>(
        app.oneshot(final_outbox_request)
            .await
            .expect("router error"),
    )
    .await;
    assert_eq!(status, StatusCode::OK);
    assert!(final_outbox.events.is_empty());
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
    let payload = serde_json::from_slice(&bytes).unwrap_or_else(|error| {
        panic!(
            "failed to deserialize response: {error} (status: {status}, body: {})",
            String::from_utf8_lossy(&bytes)
        )
    });
    (status, payload)
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateResponsePayload {
    session_id: Uuid,
    session_token: Uuid,
    status: SessionStatus,
    auth_method: AuthMethod,
    pending: bool,
    previous_session_terminated: Option<Uuid>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxResponsePayload {
    accepted: usize,
    pending_session: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxLogResponsePayload {
    session_id: Uuid,
    pending_session: bool,
    next_cursor: u64,
    events: Vec<InboxLogEventPayload>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxLogEventPayload {
    id: Uuid,
    sequence: u64,
    event_type: String,
    payload: serde_json::Value,
    #[serde(default)]
    occurred_at: Option<String>,
    received_at: String,
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
struct OutboxEnqueueResponsePayload {
    session_id: Uuid,
    event_id: Uuid,
    sequence: u64,
    pending_session: bool,
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
#[serde(rename_all = "camelCase")]
struct TradeOrderResponsePayload {
    session_id: Uuid,
    event_id: Uuid,
    sequence: u64,
    command_id: Uuid,
    pending_session: bool,
    command_type: String,
    instrument: String,
    #[serde(default)]
    order_type: Option<String>,
    #[serde(default)]
    side: Option<String>,
    #[serde(default)]
    position_id: Option<String>,
    #[serde(default)]
    volume: Option<f64>,
}

#[derive(Debug, Deserialize)]
struct ErrorResponsePayload {
    code: String,
    message: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxAckResponsePayload {
    acknowledged_event_id: Uuid,
    remaining_outbox_depth: usize,
}

async fn approve_session_via_service_bus(
    state: &AppState,
    account: &str,
    session_id: Uuid,
    auth_method: AuthMethod,
    auth_key: &str,
) {
    let fingerprint = fingerprint_for(account, auth_method, auth_key);
    match state
        .apply_admin_command(AdminCommand::Approve(AdminApprovalCommand {
            account_id: account.to_string(),
            session_id,
            auth_key_fingerprint: fingerprint,
            approved_by: Some("integration-test".to_string()),
            expires_at: None,
        }))
        .await
        .expect("service bus approval should succeed")
    {
        AdminCommandOutcome::SessionAuthenticated(_) => (),
        outcome => panic!("unexpected admin command outcome: {outcome:?}"),
    }
}

fn fingerprint_for(account: &str, method: AuthMethod, auth_key: &str) -> String {
    let storage_key = match method {
        AuthMethod::AccountSessionKey => "account_session_key",
        AuthMethod::PreSharedKey => "pre_shared_key",
    };

    let mut hasher = Sha256::new();
    hasher.update(storage_key.as_bytes());
    hasher.update(b":");
    hasher.update(account.as_bytes());
    hasher.update(b":");
    hasher.update(auth_key.as_bytes());
    format!("{:x}", hasher.finalize())
}
