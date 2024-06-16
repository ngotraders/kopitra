mod application;
mod config;
mod domain;
mod error;
mod queries;
mod routes;
mod services;
mod state;

use ntex::web;
use state::new_application_state;

#[ntex::main]
async fn main() -> std::io::Result<()> {
    web::server(|| {
        web::App::new()
            // Register swagger endpoints
            .configure(routes::openapi::ntex_config)
            .state(new_application_state)
            // Register other endpoints
            .service(
                ntex::web::scope("/api/ea")
                    .configure(routes::session::ntex_config)
                    .configure(routes::messages::ntex_config),
            )
            // Default endpoint for unregisterd endpoints
            .default_service(web::route().to(routes::default))
    })
    .bind(("0.0.0.0", 8080))?
    .run()
    .await
}
