use ntex::web;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;

/// Message
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct Message {
    pub id: String,
    pub timestamp: i64,
    #[serde(rename = "type")]
    pub message_type: String,
    pub content: String,
}

/// Messages from server
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct GetMessagesResponse {
    pub messages: Vec<Message>,
}

/// Messages to server
#[derive(Clone, Debug, Serialize, Deserialize, ToSchema)]
pub struct PostMessagesRequest {
    pub messages: Vec<Message>,
}

/// Get messages
#[utoipa::path(
  get,
  path = "/messages",
  params(
    ("X-Ea-Key" = String, Header, description = "Identification key for the trading account"),
    ("X-Ea-Version" = String, Header, description = "EA version"),
  ),
  responses(
    (status = 200, description = "Session created", body = GetMessagesResponse),
    (status = 400, description = "Invalid identification key or version"),
    (status = 401, description = "Session token is not valid"),
  ),
  security(
      ("session_token" = [])
  )
)]
#[web::get("/messages")]
pub async fn get_messages() -> web::HttpResponse {
    web::HttpResponse::Ok().finish()
}

/// Revoke session
#[utoipa::path(
  post,
  path = "/messages",
  request_body = PostMessagesRequest,
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
#[web::post("/messages")]
pub async fn post_messages() -> web::HttpResponse {
    web::HttpResponse::Ok().finish()
}

pub fn ntex_config(cfg: &mut web::ServiceConfig) {
    cfg.service(get_messages);
    cfg.service(post_messages);
}
