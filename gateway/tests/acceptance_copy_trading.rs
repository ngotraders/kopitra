use axum::{
    body::Body,
    http::{self, Request, StatusCode, header},
};
use gateway::{
    AdminApprovalCommand, AdminCommand, AdminCommandOutcome, AppState, AuthMethod,
    OutboxEnqueueResponse, SessionStatus, TradeCommandQueued, TradeCommandType, TradeOrderRequest,
    TradeOrderType, TradeSide, TradeTimeInForce,
};
use http_body_util::BodyExt;
use serde::{Deserialize, de::DeserializeOwned};
use serde_json::{Value, json};
use sha2::{Digest, Sha256};
use time::OffsetDateTime;
use tower::ServiceExt;
use uuid::Uuid;

#[tokio::test]
async fn management_can_issue_trade_commands_after_ea_connects() {
    let harness = AcceptanceHarness::new();
    let master = harness
        .create_pending_session("acct-master", "master-secret")
        .await;

    assert_eq!(master.status, SessionStatus::Pending);
    harness
        .approve_session(&master, Some("ops-console".to_string()))
        .await;
    harness.clear_initial_outbox(&master).await;

    let queued = harness
        .enqueue_trade_order(
            &master,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "USDJPY".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Buy),
                volume: Some(1.25),
                price: None,
                stop_loss: Some(131.2),
                take_profit: Some(132.75),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("master-ord-1".to_string()),
                metadata: Some(json!({
                    "groupId": "group-alpha",
                    "routing": "manual",
                })),
            },
        )
        .await;

    assert_eq!(queued.command_type, TradeCommandType::Open);
    assert_eq!(queued.instrument, "USDJPY");

    let outbox = harness.fetch_outbox(&master).await;
    assert!(!outbox.pending);
    let command_event = outbox
        .events
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .cloned()
        .expect("order command should be present");

    for event in outbox
        .events
        .iter()
        .filter(|event| event.id != command_event.id)
    {
        harness.acknowledge_outbox_via_inbox(&master, event).await;
    }
    assert_eq!(command_event.event_type, "OrderCommand");
    assert!(command_event.requires_ack);
    assert!(command_event.sequence > 0);
    assert_eq!(command_event.payload["commandType"], json!("open"));
    assert_eq!(
        command_event.payload["metadata"]["groupId"],
        json!("group-alpha")
    );

    harness
        .acknowledge_outbox_via_inbox(&master, &command_event)
        .await;

    let empty_outbox = harness.fetch_outbox(&master).await;
    assert!(
        empty_outbox
            .events
            .iter()
            .all(|event| event.event_type != "OrderCommand")
    );
}

#[tokio::test]
async fn multi_ea_group_flow_propagates_copy_trade() {
    let harness = AcceptanceHarness::new();
    let leader = harness
        .create_pending_session("acct-leader", "leader-secret")
        .await;
    let follower = harness
        .create_pending_session("acct-follower", "follower-secret")
        .await;

    harness
        .approve_session(&leader, Some("ops-console".to_string()))
        .await;
    harness.clear_initial_outbox(&leader).await;
    harness
        .approve_session(&follower, Some("ops-console".to_string()))
        .await;
    harness.clear_initial_outbox(&follower).await;

    let group_payload = json!({
        "groupId": "swing-alpha",
        "createdAt": OffsetDateTime::now_utc(),
        "members": [
            { "memberId": leader.account, "role": "leader" },
            { "memberId": follower.account, "role": "follower" },
        ],
    });

    harness
        .enqueue_outbox_event(&leader, "CopyTradeGroupUpdated", group_payload.clone())
        .await;
    harness
        .enqueue_outbox_event(&follower, "CopyTradeGroupUpdated", group_payload)
        .await;

    let queued = harness
        .enqueue_trade_order(
            &follower,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "EURUSD".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Buy),
                volume: Some(0.5),
                price: None,
                stop_loss: Some(1.0812),
                take_profit: Some(1.0965),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("follower-ord-1".to_string()),
                metadata: Some(json!({
                    "groupId": "swing-alpha",
                    "sourceAccount": leader.account,
                })),
            },
        )
        .await;

    assert_eq!(queued.command_type, TradeCommandType::Open);

    let leader_outbox = harness.fetch_outbox(&leader).await;
    assert_eq!(leader_outbox.events.len(), 1);
    assert_eq!(leader_outbox.events[0].event_type, "CopyTradeGroupUpdated");

    let follower_outbox = harness.fetch_outbox(&follower).await;
    assert_eq!(follower_outbox.events.len(), 2);
    assert_eq!(
        follower_outbox.events[0].event_type,
        "CopyTradeGroupUpdated"
    );
    assert_eq!(follower_outbox.events[1].event_type, "OrderCommand");
    assert_eq!(
        follower_outbox.events[1].payload["metadata"]["sourceAccount"],
        json!(leader.account)
    );

    harness
        .acknowledge_outbox_via_inbox(&follower, &follower_outbox.events[0])
        .await;
    harness
        .acknowledge_outbox_via_inbox(&follower, &follower_outbox.events[1])
        .await;

    let cleared = harness.fetch_outbox(&follower).await;
    assert!(cleared.events.is_empty());
}

#[tokio::test]
async fn distinct_groups_receive_independent_orders() {
    let harness = AcceptanceHarness::new();
    let follower = harness
        .create_pending_session("acct-follower", "shared-secret")
        .await;
    harness
        .approve_session(&follower, Some("ops-console".to_string()))
        .await;
    harness.clear_initial_outbox(&follower).await;

    let first_order = harness
        .enqueue_trade_order(
            &follower,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "GBPJPY".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Sell),
                volume: Some(0.75),
                price: None,
                stop_loss: Some(191.25),
                take_profit: Some(188.4),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("copy-alpha-1".to_string()),
                metadata: Some(json!({ "groupId": "copy-alpha" })),
            },
        )
        .await;

    let second_order = harness
        .enqueue_trade_order(
            &follower,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "AUDUSD".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Buy),
                volume: Some(0.4),
                price: None,
                stop_loss: Some(0.6451),
                take_profit: Some(0.6594),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("copy-beta-1".to_string()),
                metadata: Some(json!({ "groupId": "copy-beta" })),
            },
        )
        .await;

    assert_ne!(first_order.event_id, second_order.event_id);
    assert!(second_order.sequence > first_order.sequence);

    let outbox = harness.fetch_outbox(&follower).await;
    assert_eq!(outbox.events.len(), 2);
    assert_eq!(
        outbox.events[0].payload["metadata"]["groupId"],
        json!("copy-alpha")
    );
    assert_eq!(
        outbox.events[1].payload["metadata"]["groupId"],
        json!("copy-beta")
    );

    harness
        .acknowledge_outbox_via_inbox(&follower, &outbox.events[0])
        .await;
    let remaining = harness.fetch_outbox(&follower).await;
    assert_eq!(remaining.events.len(), 1);

    harness
        .acknowledge_outbox_via_inbox(&follower, &remaining.events[0])
        .await;
    let empty = harness.fetch_outbox(&follower).await;
    assert!(empty.events.is_empty());
}

#[tokio::test]
async fn shared_ea_executes_orders_for_multiple_groups() {
    let harness = AcceptanceHarness::new();
    let shared = harness
        .create_pending_session("acct-shared", "mesh-secret")
        .await;
    harness
        .approve_session(&shared, Some("ops-console".to_string()))
        .await;
    harness.clear_initial_outbox(&shared).await;

    harness
        .enqueue_outbox_event(
            &shared,
            "CopyTradeGroupUpdated",
            json!({ "groupId": "swing-alpha", "role": "leader" }),
        )
        .await;
    harness
        .enqueue_outbox_event(
            &shared,
            "CopyTradeGroupUpdated",
            json!({ "groupId": "momentum-beta", "role": "leader" }),
        )
        .await;

    let alpha_command = harness
        .enqueue_trade_order(
            &shared,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "XAUUSD".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Buy),
                volume: Some(2.0),
                price: None,
                stop_loss: Some(2321.5),
                take_profit: Some(2354.8),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("mesh-alpha-1".to_string()),
                metadata: Some(json!({ "groupId": "swing-alpha" })),
            },
        )
        .await;

    let beta_command = harness
        .enqueue_trade_order(
            &shared,
            TradeOrderRequest {
                command_type: TradeCommandType::Open,
                instrument: "NAS100".to_string(),
                order_type: Some(TradeOrderType::Market),
                side: Some(TradeSide::Sell),
                volume: Some(1.0),
                price: None,
                stop_loss: Some(17112.0),
                take_profit: Some(16840.0),
                time_in_force: Some(TradeTimeInForce::Gtc),
                position_id: None,
                client_order_id: Some("mesh-beta-1".to_string()),
                metadata: Some(json!({ "groupId": "momentum-beta" })),
            },
        )
        .await;

    let outbox = harness.fetch_outbox(&shared).await;
    assert_eq!(outbox.events.len(), 4);
    assert_eq!(outbox.events[0].payload["groupId"], json!("swing-alpha"));
    assert_eq!(outbox.events[1].payload["groupId"], json!("momentum-beta"));
    assert_eq!(
        outbox.events[2].payload["metadata"]["groupId"],
        json!("swing-alpha")
    );
    assert_eq!(
        outbox.events[3].payload["metadata"]["groupId"],
        json!("momentum-beta")
    );

    for event in outbox.events {
        harness.acknowledge_outbox_via_inbox(&shared, &event).await;
    }

    let cleared = harness.fetch_outbox(&shared).await;
    assert!(cleared.events.is_empty());

    let execution_response = harness
        .submit_inbox_events(
            &shared,
            vec![
                json!({
                    "eventType": "copy.trade.executed",
                    "payload": {
                        "groupId": "swing-alpha",
                        "commandId": alpha_command.command_id,
                        "status": "filled",
                    }
                }),
                json!({
                    "eventType": "copy.trade.executed",
                    "payload": {
                        "groupId": "momentum-beta",
                        "commandId": beta_command.command_id,
                        "status": "filled",
                    }
                }),
            ],
        )
        .await;

    assert_eq!(execution_response.accepted, 2);
    assert!(!execution_response.pending_session);
}

struct AcceptanceHarness {
    state: AppState,
    app: axum::Router,
}

impl AcceptanceHarness {
    fn new() -> Self {
        let state = AppState::default();
        let app = gateway::router(state.clone());
        Self { state, app }
    }

    async fn create_pending_session(&self, account: &str, auth_key: &str) -> SessionContext {
        let idempotency = Uuid::new_v4().to_string();
        let request = Request::builder()
            .method(http::Method::POST)
            .uri("/trade-agent/v1/sessions")
            .header(header::CONTENT_TYPE, "application/json")
            .header("X-TradeAgent-Account", account)
            .header("Idempotency-Key", idempotency)
            .body(Body::from(
                json!({
                    "authMethod": "account_session_key",
                    "authenticationKey": auth_key,
                })
                .to_string(),
            ))
            .expect("failed to build session create request");

        let (status, payload) = json_response::<SessionCreateResponsePayload>(
            self.app
                .clone()
                .oneshot(request)
                .await
                .expect("router error"),
        )
        .await;

        assert_eq!(status, StatusCode::CREATED);
        assert_eq!(payload.status, SessionStatus::Pending);

        SessionContext {
            account: account.to_string(),
            auth_key: auth_key.to_string(),
            session_id: payload.session_id,
            session_token: payload.session_token,
            auth_method: payload.auth_method,
            status: payload.status,
        }
    }

    async fn approve_session(&self, ctx: &SessionContext, approved_by: Option<String>) {
        let fingerprint = fingerprint_for(&ctx.account, ctx.auth_method, &ctx.auth_key);
        let outcome = self
            .state
            .apply_admin_command(AdminCommand::Approve(AdminApprovalCommand {
                account_id: ctx.account.clone(),
                session_id: ctx.session_id,
                auth_key_fingerprint: fingerprint,
                approved_by,
                expires_at: None,
            }))
            .await
            .expect("approval command should succeed");

        match outcome {
            AdminCommandOutcome::SessionAuthenticated(response) => {
                assert_eq!(response.status, SessionStatus::Authenticated);
                assert!(!response.pending);
            }
            other => panic!("unexpected approval outcome: {:?}", other),
        }
    }

    async fn enqueue_trade_order(
        &self,
        ctx: &SessionContext,
        request: TradeOrderRequest,
    ) -> TradeCommandQueued {
        self.state
            .enqueue_trade_command(&ctx.account, ctx.session_id, request)
            .await
            .expect("trade command enqueue should succeed")
    }

    async fn enqueue_outbox_event(
        &self,
        ctx: &SessionContext,
        event_type: &str,
        payload: Value,
    ) -> OutboxEnqueueResponse {
        self.state
            .enqueue_outbox_event(
                &ctx.account,
                ctx.session_id,
                gateway::OutboxEventRequest {
                    event_type: event_type.to_string(),
                    payload,
                    requires_ack: true,
                },
            )
            .await
            .expect("outbox enqueue should succeed")
    }

    async fn fetch_outbox(&self, ctx: &SessionContext) -> OutboxResponsePayload {
        let request = Request::builder()
            .method(http::Method::GET)
            .uri("/trade-agent/v1/sessions/current/outbox")
            .header("X-TradeAgent-Account", &ctx.account)
            .header(
                header::AUTHORIZATION,
                format!("Bearer {}", ctx.session_token),
            )
            .body(Body::empty())
            .expect("failed to build outbox request");

        let (status, body) = json_response::<OutboxResponsePayload>(
            self.app
                .clone()
                .oneshot(request)
                .await
                .expect("router error"),
        )
        .await;

        assert_eq!(status, StatusCode::OK);
        body
    }

    async fn acknowledge_outbox_via_inbox(
        &self,
        ctx: &SessionContext,
        event: &OutboxEventPayload,
    ) -> InboxResponsePayload {
        let idempotency = Uuid::new_v4().to_string();
        let request = Request::builder()
            .method(http::Method::POST)
            .uri("/trade-agent/v1/sessions/current/inbox")
            .header(header::CONTENT_TYPE, "application/json")
            .header("X-TradeAgent-Account", &ctx.account)
            .header("Idempotency-Key", idempotency)
            .header(
                header::AUTHORIZATION,
                format!("Bearer {}", ctx.session_token),
            )
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
            .expect("failed to build inbox request");

        let (status, response) = json_response::<InboxResponsePayload>(
            self.app
                .clone()
                .oneshot(request)
                .await
                .expect("router error"),
        )
        .await;

        assert_eq!(status, StatusCode::ACCEPTED);
        response
    }

    async fn clear_initial_outbox(&self, ctx: &SessionContext) {
        let snapshot = self.fetch_outbox(ctx).await;
        for event in snapshot.events {
            self.acknowledge_outbox_via_inbox(ctx, &event).await;
        }
    }

    async fn submit_inbox_events(
        &self,
        ctx: &SessionContext,
        events: Vec<Value>,
    ) -> InboxResponsePayload {
        let idempotency = Uuid::new_v4().to_string();
        let request = Request::builder()
            .method(http::Method::POST)
            .uri("/trade-agent/v1/sessions/current/inbox")
            .header(header::CONTENT_TYPE, "application/json")
            .header("X-TradeAgent-Account", &ctx.account)
            .header("Idempotency-Key", idempotency)
            .header(
                header::AUTHORIZATION,
                format!("Bearer {}", ctx.session_token),
            )
            .body(Body::from(json!({ "events": events }).to_string()))
            .expect("failed to build inbox request");

        let (status, response) = json_response::<InboxResponsePayload>(
            self.app
                .clone()
                .oneshot(request)
                .await
                .expect("router error"),
        )
        .await;

        assert_eq!(status, StatusCode::ACCEPTED);
        response
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

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateResponsePayload {
    session_id: Uuid,
    session_token: Uuid,
    status: SessionStatus,
    auth_method: AuthMethod,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxResponsePayload {
    pending: bool,
    events: Vec<OutboxEventPayload>,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxEventPayload {
    id: Uuid,
    sequence: u64,
    event_type: String,
    payload: Value,
    requires_ack: bool,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct InboxResponsePayload {
    accepted: usize,
    pending_session: bool,
}

struct SessionContext {
    account: String,
    auth_key: String,
    session_id: Uuid,
    session_token: Uuid,
    auth_method: AuthMethod,
    status: SessionStatus,
}

async fn json_response<T>(response: axum::response::Response) -> (StatusCode, T)
where
    T: DeserializeOwned,
{
    let status = response.status();
    let body = response
        .into_body()
        .collect()
        .await
        .expect("failed to read response body")
        .to_bytes();
    let parsed = serde_json::from_slice(&body).expect("failed to deserialize response body");
    (status, parsed)
}
