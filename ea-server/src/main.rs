mod application;
mod config;
mod domain;
mod error;
mod persist;
mod queries;
mod routes;
mod services;

use application::ApplicationState;
use ntex::web;

#[ntex::main]
async fn main() -> std::io::Result<()> {
    web::server(|| {
        web::App::new()
            // Register swagger endpoints
            .configure(routes::openapi::ntex_config)
            .state(ApplicationState::new)
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

#[cfg(test)]
mod testing;
