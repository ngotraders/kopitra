use std::{env, time::Duration};

use anyhow::{Context, Result};
use once_cell::sync::{Lazy, OnceCell};
use reqwest::{Client, Url};
use serde::Deserialize;
use serde_json::{json, Value};
use sha2::{Digest, Sha256};
use tokio::{sync::Mutex, time::sleep};
use uuid::Uuid;

static TEST_GUARD: Lazy<Mutex<()>> = Lazy::new(|| Mutex::new(()));
static TRACING: OnceCell<()> = OnceCell::new();

#[tokio::test]
async fn scenario_one_single_ea_trade_flow() -> Result<()> {
    let _guard = TEST_GUARD.lock().await;
    Harness::init_tracing();

    let harness = Harness::new().await?;
    let master = harness.connect_ea("acct-master", "master-secret").await?;
    harness.clear_outbox(&master).await?;
    harness
        .approve_session(&master, Some("ops-console"))
        .await?;
    harness.assert_session_authenticated(&master).await?;

    harness
        .enqueue_trade_order(
            &master,
            TradeCommand {
                command_type: "open",
                instrument: "USDJPY",
                order_type: Some("market"),
                side: Some("buy"),
                volume: Some(1.0),
                price: None,
                stop_loss: Some(131.2),
                take_profit: Some(132.75),
                time_in_force: Some("gtc"),
                position_id: None,
                client_order_id: Some("master-open-1".to_string()),
                metadata: Some(json!({ "source": "ops-console" })),
            },
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let events = harness.fetch_outbox(&master).await?;
    let order = events
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .context("expected trade order in outbox")?;
    assert_eq!(order.payload["commandType"], json!("open"));
    assert_eq!(order.payload["instrument"], json!("USDJPY"));
    harness.ack_events(&master, &events).await?;

    harness
        .enqueue_trade_order(
            &master,
            TradeCommand {
                command_type: "close",
                instrument: "USDJPY",
                order_type: Some("market"),
                side: Some("sell"),
                volume: Some(1.0),
                price: None,
                stop_loss: None,
                take_profit: None,
                time_in_force: Some("ioc"),
                position_id: Some("pos-master-1"),
                client_order_id: Some("master-close-1".to_string()),
                metadata: Some(json!({ "source": "ops-console" })),
            },
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let close_events = harness.fetch_outbox(&master).await?;
    let close_order = close_events
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .context("expected close order in outbox")?;
    assert_eq!(close_order.payload["commandType"], json!("close"));
    assert_eq!(close_order.payload["positionId"], json!("pos-master-1"));
    harness.ack_events(&master, &close_events).await?;

    Ok(())
}

#[tokio::test]
async fn scenario_two_copy_trade_flow() -> Result<()> {
    let _guard = TEST_GUARD.lock().await;
    Harness::init_tracing();

    let harness = Harness::new().await?;
    let leader = harness.connect_ea("acct-leader", "leader-secret").await?;
    let follower = harness
        .connect_ea("acct-follower", "follower-secret")
        .await?;

    harness.clear_outbox(&leader).await?;
    harness.clear_outbox(&follower).await?;

    harness
        .approve_session(&leader, Some("ops-console"))
        .await?;
    harness
        .approve_session(&follower, Some("ops-console"))
        .await?;
    harness.assert_session_authenticated(&leader).await?;
    harness.assert_session_authenticated(&follower).await?;

    harness
        .create_copy_group("swing-alpha", "Swing Alpha", "ops-console")
        .await?;
    harness
        .upsert_group_member(
            "swing-alpha",
            &leader,
            "leader",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "swing-alpha",
            &follower,
            "follower",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let follower_updates = harness.fetch_outbox(&follower).await?;
    let group_event = follower_updates
        .iter()
        .find(|event| event.event_type == "CopyTradeGroupUpdated")
        .context("expected copy trade update for follower")?;
    assert_eq!(group_event.payload["groupId"], json!("swing-alpha"));
    harness.ack_events(&follower, &follower_updates).await?;

    harness
        .execute_copy_trade(
            "swing-alpha",
            CopyTradeExecution {
                source_account: &leader.account,
                initiated_by: Some("ops-console"),
                command: TradeCommand {
                    command_type: "open",
                    instrument: "EURUSD",
                    order_type: Some("market"),
                    side: Some("buy"),
                    volume: Some(0.5),
                    price: None,
                    stop_loss: Some(1.0812),
                    take_profit: Some(1.0965),
                    time_in_force: Some("gtc"),
                    position_id: None,
                    client_order_id: Some("copy-ord-1".to_string()),
                    metadata: Some(json!({"strategy": "swing"})),
                },
            },
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let follower_orders = harness.fetch_outbox(&follower).await?;
    let copy_order = follower_orders
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .context("expected copy trade order")?;
    assert_eq!(
        copy_order.payload["metadata"]["groupId"],
        json!("swing-alpha")
    );
    assert_eq!(
        copy_order.payload["metadata"]["sourceAccount"],
        json!(leader.account)
    );
    harness.ack_events(&follower, &follower_orders).await?;

    Ok(())
}

#[tokio::test]
async fn scenario_three_multiple_groups_independent_orders() -> Result<()> {
    let _guard = TEST_GUARD.lock().await;
    Harness::init_tracing();

    let harness = Harness::new().await?;
    let leader_a = harness
        .connect_ea("acct-alpha-leader", "alpha-secret")
        .await?;
    let follower_a = harness
        .connect_ea("acct-alpha-follower", "alpha-follow")
        .await?;
    let leader_b = harness
        .connect_ea("acct-beta-leader", "beta-secret")
        .await?;
    let follower_b = harness
        .connect_ea("acct-beta-follower", "beta-follow")
        .await?;

    for session in [&leader_a, &follower_a, &leader_b, &follower_b] {
        harness.clear_outbox(session).await?;
        harness
            .approve_session(session, Some("ops-console"))
            .await?;
        harness.assert_session_authenticated(session).await?;
    }

    harness
        .create_copy_group("momentum-alpha", "Momentum Alpha", "ops-console")
        .await?;
    harness
        .create_copy_group("momentum-beta", "Momentum Beta", "ops-console")
        .await?;

    harness
        .upsert_group_member(
            "momentum-alpha",
            &leader_a,
            "leader",
            "aggressive",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "momentum-alpha",
            &follower_a,
            "follower",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "momentum-beta",
            &leader_b,
            "leader",
            "conservative",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "momentum-beta",
            &follower_b,
            "follower",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    for session in [&follower_a, &follower_b] {
        let updates = harness.fetch_outbox(session).await?;
        harness.ack_events(session, &updates).await?;
    }

    harness
        .execute_copy_trade(
            "momentum-alpha",
            CopyTradeExecution {
                source_account: &leader_a.account,
                initiated_by: Some("ops-alpha"),
                command: TradeCommand {
                    command_type: "open",
                    instrument: "GBPUSD",
                    order_type: Some("market"),
                    side: Some("buy"),
                    volume: Some(0.8),
                    price: None,
                    stop_loss: Some(1.2512),
                    take_profit: Some(1.2695),
                    time_in_force: Some("gtc"),
                    position_id: None,
                    client_order_id: Some("alpha-ord-1".to_string()),
                    metadata: Some(json!({"campaign": "momentum-alpha"})),
                },
            },
        )
        .await?;

    harness
        .execute_copy_trade(
            "momentum-beta",
            CopyTradeExecution {
                source_account: &leader_b.account,
                initiated_by: Some("ops-beta"),
                command: TradeCommand {
                    command_type: "open",
                    instrument: "AUDUSD",
                    order_type: Some("market"),
                    side: Some("sell"),
                    volume: Some(0.4),
                    price: None,
                    stop_loss: Some(0.6652),
                    take_profit: Some(0.6511),
                    time_in_force: Some("gtc"),
                    position_id: None,
                    client_order_id: Some("beta-ord-1".to_string()),
                    metadata: Some(json!({"campaign": "momentum-beta"})),
                },
            },
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let orders_a = harness.fetch_outbox(&follower_a).await?;
    let order_a = orders_a
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .context("expected follower A order")?;
    assert_eq!(order_a.payload["instrument"], json!("GBPUSD"));
    assert_eq!(
        order_a.payload["metadata"]["groupId"],
        json!("momentum-alpha")
    );
    harness.ack_events(&follower_a, &orders_a).await?;

    let orders_b = harness.fetch_outbox(&follower_b).await?;
    let order_b = orders_b
        .iter()
        .find(|event| event.event_type == "OrderCommand")
        .context("expected follower B order")?;
    assert_eq!(order_b.payload["instrument"], json!("AUDUSD"));
    assert_eq!(
        order_b.payload["metadata"]["groupId"],
        json!("momentum-beta")
    );
    harness.ack_events(&follower_b, &orders_b).await?;

    Ok(())
}

#[tokio::test]
async fn scenario_four_multi_group_membership_executes_all() -> Result<()> {
    let _guard = TEST_GUARD.lock().await;
    Harness::init_tracing();

    let harness = Harness::new().await?;
    let leader_primary = harness
        .connect_ea("acct-swing-leader", "swing-secret")
        .await?;
    let leader_secondary = harness
        .connect_ea("acct-hedge-leader", "hedge-secret")
        .await?;
    let follower_shared = harness
        .connect_ea("acct-shared-follower", "shared-secret")
        .await?;

    for session in [&leader_primary, &leader_secondary, &follower_shared] {
        harness.clear_outbox(session).await?;
        harness
            .approve_session(session, Some("ops-console"))
            .await?;
        harness.assert_session_authenticated(session).await?;
    }

    harness
        .create_copy_group("swing-cadre", "Swing Cadre", "ops-console")
        .await?;
    harness
        .create_copy_group("hedge-cadre", "Hedge Cadre", "ops-console")
        .await?;

    harness
        .upsert_group_member(
            "swing-cadre",
            &leader_primary,
            "leader",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "swing-cadre",
            &follower_shared,
            "follower",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "hedge-cadre",
            &leader_secondary,
            "leader",
            "conservative",
            1.0,
            "ops-console",
        )
        .await?;
    harness
        .upsert_group_member(
            "hedge-cadre",
            &follower_shared,
            "follower",
            "balanced",
            1.0,
            "ops-console",
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let updates = harness.fetch_outbox(&follower_shared).await?;
    harness.ack_events(&follower_shared, &updates).await?;

    harness
        .execute_copy_trade(
            "swing-cadre",
            CopyTradeExecution {
                source_account: &leader_primary.account,
                initiated_by: Some("ops-swing"),
                command: TradeCommand {
                    command_type: "open",
                    instrument: "NZDJPY",
                    order_type: Some("market"),
                    side: Some("buy"),
                    volume: Some(1.2),
                    price: None,
                    stop_loss: Some(88.15),
                    take_profit: Some(91.65),
                    time_in_force: Some("gtc"),
                    position_id: None,
                    client_order_id: Some("swing-order-1".to_string()),
                    metadata: Some(json!({"playbook": "swing"})),
                },
            },
        )
        .await?;

    harness
        .execute_copy_trade(
            "hedge-cadre",
            CopyTradeExecution {
                source_account: &leader_secondary.account,
                initiated_by: Some("ops-hedge"),
                command: TradeCommand {
                    command_type: "open",
                    instrument: "USDCHF",
                    order_type: Some("market"),
                    side: Some("sell"),
                    volume: Some(0.6),
                    price: None,
                    stop_loss: Some(0.8925),
                    take_profit: Some(0.8742),
                    time_in_force: Some("gtc"),
                    position_id: None,
                    client_order_id: Some("hedge-order-1".to_string()),
                    metadata: Some(json!({"playbook": "hedge"})),
                },
            },
        )
        .await?;

    sleep(Duration::from_millis(200)).await;
    let shared_orders = harness.fetch_outbox(&follower_shared).await?;
    let swing_order = shared_orders
        .iter()
        .find(|event| {
            event.event_type == "OrderCommand"
                && event.payload["metadata"]["groupId"] == json!("swing-cadre")
        })
        .context("expected swing cadre order")?;
    let hedge_order = shared_orders
        .iter()
        .find(|event| {
            event.event_type == "OrderCommand"
                && event.payload["metadata"]["groupId"] == json!("hedge-cadre")
        })
        .context("expected hedge cadre order")?;

    assert_eq!(swing_order.payload["instrument"], json!("NZDJPY"));
    assert_eq!(hedge_order.payload["instrument"], json!("USDCHF"));
    harness.ack_events(&follower_shared, &shared_orders).await?;

    Ok(())
}

struct Harness {
    client: Client,
    gateway_base: Url,
    management_base: Url,
    ops_token: String,
}

impl Harness {
    fn init_tracing() {
        TRACING.get_or_init(|| {
            let _ = tracing_subscriber::fmt()
                .with_env_filter(
                    tracing_subscriber::EnvFilter::try_from_default_env()
                        .unwrap_or_else(|_| "info".into()),
                )
                .try_init();
        });
    }

    async fn new() -> Result<Self> {
        let gateway_base =
            env::var("GATEWAY_BASE_URL").unwrap_or_else(|_| "http://gateway:8080".to_string());
        let management_base = env::var("MANAGEMENT_BASE_URL")
            .unwrap_or_else(|_| "http://management:7071/api".to_string());
        let ops_token = env::var("OPS_BEARER_TOKEN").unwrap_or_else(|_| "dev-token".to_string());

        let client = Client::builder().timeout(Duration::from_secs(10)).build()?;

        Ok(Self {
            client,
            gateway_base: Url::parse(&gateway_base)?,
            management_base: Url::parse(&management_base)?,
            ops_token,
        })
    }

    fn gateway_url(&self, path: &str) -> Result<Url> {
        self.gateway_base.join(path).context("building gateway url")
    }

    fn management_url(&self, path: &str) -> Result<Url> {
        self.management_base
            .join(path)
            .context("building management url")
    }

    async fn connect_ea(&self, account: &str, auth_key: &str) -> Result<EaSession> {
        let url = self.gateway_url("/trade-agent/v1/sessions")?;
        let response = self
            .client
            .post(url)
            .header("X-TradeAgent-Account", account)
            .header("Idempotency-Key", Uuid::new_v4().to_string())
            .json(&json!({
                "authMethod": "account_session_key",
                "authenticationKey": auth_key,
            }))
            .send()
            .await?
            .error_for_status()?;

        let created: SessionCreateResponse = response.json().await?;
        let fingerprint = hash_secret(auth_key, account);

        Ok(EaSession {
            account: account.to_string(),
            auth_fingerprint: fingerprint,
            session_id: created.session_id,
            session_token: created.session_token,
        })
    }

    async fn clear_outbox(&self, session: &EaSession) -> Result<()> {
        let events = self.fetch_outbox(session).await?;
        self.ack_events(session, &events).await
    }

    async fn approve_session(&self, session: &EaSession, approved_by: Option<&str>) -> Result<()> {
        let url = self.management_url(&format!(
            "/admin/experts/{}/sessions/{}/approve",
            session.account, session.session_id
        ))?;
        self.client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.ops_token))
            .header("X-TradeAgent-Account", "console")
            .json(&json!({
                "accountId": session.account,
                "authKeyFingerprint": session.auth_fingerprint,
                "approvedBy": approved_by,
            }))
            .send()
            .await?
            .error_for_status()?;
        Ok(())
    }

    async fn assert_session_authenticated(&self, session: &EaSession) -> Result<()> {
        let url = self.management_url(&format!(
            "/trade-agent/v1/admin/accounts/{}/sessions/active",
            session.account
        ))?;
        let summary: SessionSummary = self
            .client
            .get(url)
            .send()
            .await?
            .error_for_status()?
            .json()
            .await?;
        anyhow::ensure!(
            summary.status == SessionStatus::Authenticated,
            "session for {} not authenticated (status: {:?})",
            session.account,
            summary.status
        );
        Ok(())
    }

    async fn enqueue_trade_order(
        &self,
        session: &EaSession,
        command: TradeCommand<'_>,
    ) -> Result<()> {
        let url = self.management_url(&format!(
            "/admin/experts/{}/sessions/{}/trade-orders",
            session.account, session.session_id
        ))?;

        let mut body = json!({
            "accountId": session.account,
            "commandType": command.command_type,
            "instrument": command.instrument,
        });

        if let Some(value) = command.order_type {
            body["orderType"] = json!(value);
        }
        if let Some(value) = command.side {
            body["side"] = json!(value);
        }
        if let Some(value) = command.volume {
            body["volume"] = json!(value);
        }
        if let Some(value) = command.price {
            body["price"] = json!(value);
        }
        if let Some(value) = command.stop_loss {
            body["stopLoss"] = json!(value);
        }
        if let Some(value) = command.take_profit {
            body["takeProfit"] = json!(value);
        }
        if let Some(value) = command.time_in_force {
            body["timeInForce"] = json!(value);
        }
        if let Some(value) = command.position_id {
            body["positionId"] = json!(value);
        }
        if let Some(value) = command.client_order_id {
            body["clientOrderId"] = json!(value);
        }
        if let Some(metadata) = command.metadata {
            body["metadata"] = metadata;
        }

        self.client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.ops_token))
            .header("X-TradeAgent-Account", "console")
            .json(&body)
            .send()
            .await?
            .error_for_status()?;
        Ok(())
    }

    async fn create_copy_group(
        &self,
        group_id: &str,
        name: &str,
        requested_by: &str,
    ) -> Result<()> {
        let url = self.management_url("/admin/copy-trade/groups")?;
        self.client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.ops_token))
            .header("X-TradeAgent-Account", "console")
            .json(&json!({
                "groupId": group_id,
                "name": name,
                "description": format!("Group {name}"),
                "requestedBy": requested_by,
            }))
            .send()
            .await?
            .error_for_status()?;
        Ok(())
    }

    async fn upsert_group_member(
        &self,
        group_id: &str,
        session: &EaSession,
        role: &str,
        risk_strategy: &str,
        allocation: f64,
        requested_by: &str,
    ) -> Result<()> {
        let url = self.management_url(&format!(
            "/admin/copy-trade/groups/{}/members/{}",
            group_id, session.account
        ))?;
        self.client
            .put(url)
            .header("Authorization", format!("Bearer {}", self.ops_token))
            .header("X-TradeAgent-Account", "console")
            .json(&json!({
                "role": role,
                "riskStrategy": risk_strategy,
                "allocation": allocation,
                "requestedBy": requested_by,
            }))
            .send()
            .await?
            .error_for_status()?;
        Ok(())
    }

    async fn execute_copy_trade(
        &self,
        group_id: &str,
        execution: CopyTradeExecution<'_>,
    ) -> Result<()> {
        let url = self.management_url(&format!("/admin/copy-trade/groups/{group_id}/orders"))?;
        let mut body = json!({
            "sourceAccount": execution.source_account,
            "commandType": execution.command.command_type,
            "instrument": execution.command.instrument,
        });

        if let Some(value) = execution.command.order_type {
            body["orderType"] = json!(value);
        }
        if let Some(value) = execution.command.side {
            body["side"] = json!(value);
        }
        if let Some(value) = execution.command.volume {
            body["volume"] = json!(value);
        }
        if let Some(value) = execution.command.price {
            body["price"] = json!(value);
        }
        if let Some(value) = execution.command.stop_loss {
            body["stopLoss"] = json!(value);
        }
        if let Some(value) = execution.command.take_profit {
            body["takeProfit"] = json!(value);
        }
        if let Some(value) = execution.command.time_in_force {
            body["timeInForce"] = json!(value);
        }
        if let Some(value) = execution.command.position_id {
            body["positionId"] = json!(value);
        }
        if let Some(value) = execution.command.client_order_id {
            body["clientOrderId"] = json!(value);
        }
        if let Some(value) = execution.initiated_by {
            body["initiatedBy"] = json!(value);
        }
        if let Some(metadata) = execution.command.metadata {
            body["metadata"] = metadata;
        }

        self.client
            .post(url)
            .header("Authorization", format!("Bearer {}", self.ops_token))
            .header("X-TradeAgent-Account", "console")
            .json(&body)
            .send()
            .await?
            .error_for_status()?;
        Ok(())
    }

    async fn fetch_outbox(&self, session: &EaSession) -> Result<Vec<OutboundEvent>> {
        let url = self.gateway_url("/trade-agent/v1/sessions/current/outbox")?;
        let response = self
            .client
            .get(url)
            .header("X-TradeAgent-Account", &session.account)
            .header("Authorization", format!("Bearer {}", session.session_token))
            .send()
            .await?
            .error_for_status()?;

        let payload: OutboxFetchResponse = response.json().await?;
        Ok(payload.events)
    }

    async fn ack_events(&self, session: &EaSession, events: &[OutboundEvent]) -> Result<()> {
        for event in events {
            if !event.requires_ack {
                continue;
            }
            let url = self.gateway_url(&format!(
                "/trade-agent/v1/sessions/current/outbox/{}/ack",
                event.id
            ))?;
            self.client
                .post(url)
                .header("X-TradeAgent-Account", &session.account)
                .header("Authorization", format!("Bearer {}", session.session_token))
                .header("Idempotency-Key", Uuid::new_v4().to_string())
                .send()
                .await?
                .error_for_status()?;
        }
        Ok(())
    }
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionCreateResponse {
    session_id: Uuid,
    session_token: Uuid,
}

#[derive(Debug, Clone, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "snake_case")]
enum SessionStatus {
    Pending,
    Authenticated,
    Terminated,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct SessionSummary {
    status: SessionStatus,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboxFetchResponse {
    events: Vec<OutboundEvent>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct OutboundEvent {
    id: Uuid,
    event_type: String,
    payload: Value,
    requires_ack: bool,
}

struct EaSession {
    account: String,
    auth_fingerprint: String,
    session_id: Uuid,
    session_token: Uuid,
}

struct TradeCommand<'a> {
    command_type: &'a str,
    instrument: &'a str,
    order_type: Option<&'a str>,
    side: Option<&'a str>,
    volume: Option<f64>,
    price: Option<f64>,
    stop_loss: Option<f64>,
    take_profit: Option<f64>,
    time_in_force: Option<&'a str>,
    position_id: Option<&'a str>,
    client_order_id: Option<String>,
    metadata: Option<Value>,
}

struct CopyTradeExecution<'a> {
    source_account: &'a str,
    initiated_by: Option<&'a str>,
    command: TradeCommand<'a>,
}

fn hash_secret(secret: &str, account: &str) -> String {
    let mut hasher = Sha256::new();
    hasher.update(b"account_session_key");
    hasher.update(b":");
    hasher.update(account.as_bytes());
    hasher.update(b":");
    hasher.update(secret.as_bytes());
    format!("{:x}", hasher.finalize())
}
