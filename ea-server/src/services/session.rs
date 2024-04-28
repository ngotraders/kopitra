use ntex::web;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;

/// Position
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct Position {
    /// Id for copy trade
    pub trade_id: String,
    /// Local ticket no for the position
    pub ticket_no: String,
    /// Symbol
    pub symbol: String,
    /// Trade type (buy or sell)
    pub trade_type: String,
    /// Remaining lots
    pub quantity: f64,
}

/// Order to open
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct CopyTradeOpenOrder {
    /// Id for copy trade
    pub trade_id: String,
    /// Symbol
    pub symbol: String,
    /// Trade type (buy or sell)
    pub trade_type: String,
    /// Publisher order price opened
    pub price: Option<f64>,
    /// Publisher specified balance to order ratio
    pub percentage: f64,
}

/// Order to open
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct CopyTradeCloseOrder {
    /// Id for copy trade
    pub trade_id: String,
    /// Publisher order price closed
    pub price: Option<f64>,
}

/// Start session response
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct StartSessionResponse {
    /// session token
    pub token: String,
    /// prefered role for account
    pub role: Option<String>,
    /// server confirmed positions
    pub positions: Vec<Position>,
    /// server confirmed pending orders for open
    pub pending_order_open: Vec<CopyTradeOpenOrder>,
    /// server confirmed pending orders for close
    pub pending_order_close: Vec<CopyTradeCloseOrder>,
}

/// Revoke session request
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct RevokeSessionRequest {
    /// reason of revoking session
    pub reason: String,
}

/// Start session
#[utoipa::path(
  post,
  path = "/session",
  params(
    ("X-Ea-Key" = String, Header, description = "Identification key for the trading account"),
    ("X-Ea-Version" = String, Header, description = "EA version"),
  ),
  responses(
    (status = 200, description = "Session created", body = StartSessionResponse),
    (status = 400, description = "Invalid identification key or version"),
    (status = 403, description = "Session already exists"),
  ),
)]
#[web::post("/session")]
pub async fn start_session() -> web::HttpResponse {
    web::HttpResponse::Ok().finish()
}

/// Revoke session
#[utoipa::path(
  post,
  path = "/session/revoke",
  request_body = RevokeSessionRequest,
  params(
    ("X-Ea-Key" = String, Header, description = "Identification key for the trading account"),
    ("X-Ea-Version" = String, Header, description = "EA version"),
  ),
  responses(
    (status = 200, description = "Session revoked"),
    (status = 400, description = "Invalid identification key or version"),
    (status = 401, description = "Session token is not valid"),
  ),
  security(
      ("session_token" = [])
  )
)]
#[web::post("/session/revoke")]
pub async fn revoke_session() -> web::HttpResponse {
    web::HttpResponse::Ok().finish()
}

pub fn ntex_config(cfg: &mut web::ServiceConfig) {
    cfg.service(start_session);
    cfg.service(revoke_session);
}
