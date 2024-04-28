pub mod openapi;
pub mod session;
pub mod messages;

use ntex::web;

pub async fn default() -> web::HttpResponse {
    web::HttpResponse::NotFound().finish()
}
