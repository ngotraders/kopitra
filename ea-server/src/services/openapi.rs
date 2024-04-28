use std::sync::Arc;

use ntex::http;
use ntex::util::Bytes;
use ntex::web;
use utoipa::openapi::security::{HttpAuthScheme, HttpBuilder, SecurityScheme};
use utoipa::{Modify, OpenApi};

use super::messages::{GetMessagesResponse, Message, PostMessagesRequest};
use super::session::{
    CopyTradeCloseOrder, CopyTradeOpenOrder, Position, RevokeSessionRequest, StartSessionResponse,
};
use crate::error::HttpError;

use super::messages;
use super::session;

/// Main structure to generate OpenAPI documentation
#[derive(OpenApi)]
#[openapi(
    info(title = "FX Copy Trading API",),
    servers((url = "http://localhost:8080/api/ea/")),
    paths(
        session::start_session,
        session::revoke_session,
        messages::get_messages,
        messages::post_messages,
    ),
    components(
        schemas(
            Position,
            CopyTradeOpenOrder,
            CopyTradeCloseOrder,
            StartSessionResponse,
            RevokeSessionRequest,
            Message,
            GetMessagesResponse,
            PostMessagesRequest
        )
    ),
    modifiers(&SecurityAddon)
)]
pub(crate) struct ApiDoc;

struct SecurityAddon;

impl Modify for SecurityAddon {
    fn modify(&self, openapi: &mut utoipa::openapi::OpenApi) {
        let components = openapi.components.as_mut().unwrap(); // we can unwrap safely since there already is components registered.
        components.add_security_scheme(
            "session_token",
            SecurityScheme::Http(
                HttpBuilder::new()
                    .scheme(HttpAuthScheme::Bearer)
                    .description(Some("Bearer with session token"))
                    .build(),
            ),
        )
    }
}

#[web::get("/{tail}*")]
async fn get_swagger(
    tail: web::types::Path<String>,
    openapi_conf: web::types::State<Arc<utoipa_swagger_ui::Config<'static>>>,
) -> Result<web::HttpResponse, HttpError> {
    if tail.as_ref() == "swagger.json" {
        let spec = ApiDoc::openapi().to_json().map_err(|err| HttpError {
            status: http::StatusCode::INTERNAL_SERVER_ERROR,
            msg: format!("Error generating OpenAPI spec: {}", err),
        })?;
        return Ok(web::HttpResponse::Ok()
            .content_type("application/json")
            .body(spec));
    }
    let conf = openapi_conf.as_ref().clone();
    match utoipa_swagger_ui::serve(&tail, conf.into()).map_err(|err| HttpError {
        msg: format!("Error serving Swagger UI: {}", err),
        status: http::StatusCode::INTERNAL_SERVER_ERROR,
    })? {
        None => Err(HttpError {
            status: http::StatusCode::NOT_FOUND,
            msg: format!("path not found: {}", tail),
        }),
        Some(file) => Ok({
            let bytes = Bytes::from(file.bytes.to_vec());
            web::HttpResponse::Ok()
                .content_type(file.content_type)
                .body(bytes)
        }),
    }
}

pub fn ntex_config(config: &mut web::ServiceConfig) {
    let swagger_config =
        Arc::new(utoipa_swagger_ui::Config::new(["/explorer/swagger.json"]).use_base_layout());
    config.service(
        web::scope("/explorer/")
            .state(swagger_config)
            .service(get_swagger),
    );
}
