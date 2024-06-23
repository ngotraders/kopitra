use std::str;

use ntex::web;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;

use crate::application::ApplicationState;

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
pub struct OpenSessionResponse {
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

/// Close session request
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct CloseSessionRequest {
    /// reason of close session
    pub reason: String,
}

#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct ErrorResponse {
    pub message: String,
}

/// Open session
#[utoipa::path(
  post,
  path = "/session",
  params(
    ("X-Ea-Key" = String, Header, description = "Identification key for the trading account"),
    ("X-Ea-Version" = String, Header, description = "EA version"),
  ),
  responses(
    (status = 200, description = "OK", body = OpenSessionResponse),
    (status = 400, description = "Invalid identification key or version"),
    (status = 403, description = "Session already exists"),
  ),
)]
#[web::post("/session")]
pub async fn open_session(
    req: web::HttpRequest,
    state: web::types::State<ApplicationState>,
) -> web::HttpResponse {
    let ea_key: &str;
    match req.headers().get("X-Ea-Key") {
        None => return web::HttpResponse::BadRequest().finish(),
        Some(value) => {
            ea_key = value
                .to_str()
                .expect("X-Ea-Key cannot be handled as string")
        }
    };
    let ea_version: &str;
    match req.headers().get("X-Ea-Version") {
        None => return web::HttpResponse::BadRequest().finish(),
        Some(value) => {
            ea_version = value
                .to_str()
                .expect("X-Ea-Version cannot be handled as string")
        }
    };
    let result = state.open_session(ea_key, ea_version).await;
    match result {
        Err(message) => web::HttpResponse::BadRequest().json(&ErrorResponse {
            message: message.to_string(),
        }),
        Ok(session_token) => web::HttpResponse::Ok().json(&OpenSessionResponse {
            token: session_token,
            role: None,
            positions: [].to_vec(),
            pending_order_open: [].to_vec(),
            pending_order_close: [].to_vec(),
        }),
    }
}

/// Close session
#[utoipa::path(
  delete,
  path = "/session",
  params(
    ("X-Ea-Key" = String, Header, description = "Identification key for the trading account"),
    ("X-Ea-Version" = String, Header, description = "EA version"),
    ("Reason" = Option<String>, Query, description = "Reason for closing session")
  ),
  responses(
    (status = 200, description = "OK"),
    (status = 400, description = "Invalid identification key or version"),
    (status = 401, description = "Session token is not valid"),
  ),
  security(
      ("session_token" = [])
  )
)]
#[web::delete("/session")]
pub async fn close_session(
    req: web::HttpRequest,
    state: web::types::State<ApplicationState>,
) -> web::HttpResponse {
    let ea_key: &str;
    match req.headers().get("X-Ea-Key") {
        None => return web::HttpResponse::BadRequest().finish(),
        Some(value) => {
            ea_key = value
                .to_str()
                .expect("X-Ea-Key cannot be handled as string")
        }
    };
    let ea_version: &str;
    match req.headers().get("X-Ea-Version") {
        None => return web::HttpResponse::BadRequest().finish(),
        Some(value) => {
            ea_version = value
                .to_str()
                .expect("X-Ea-Version cannot be handled as string")
        }
    };
    let auth_header_value: &str;
    match req.headers().get("Authorization") {
        None => return web::HttpResponse::BadRequest().finish(),
        Some(value) => {
            auth_header_value = value
                .to_str()
                .expect("Authorization cannot be handled as string")
        }
    };
    if !auth_header_value.starts_with("Bearer ") {
        return web::HttpResponse::BadRequest().finish();
    }
    let auth_token = &auth_header_value[7..auth_header_value.len()];

    let result = state.close_session(auth_token, ea_key, ea_version).await;
    match result {
        Err(message) => web::HttpResponse::BadRequest().json(&ErrorResponse {
            message: message.to_string(),
        }),
        Ok(_) => web::HttpResponse::Ok().finish(),
    }
}

pub fn ntex_config(cfg: &mut web::ServiceConfig) {
    cfg.service(open_session);
    cfg.service(close_session);
}
