use std::sync::Arc;

use cqrs_es::Query;
use postgres_es::{PostgresCqrs, PostgresViewRepository};
use sqlx::{Pool, Postgres};

use crate::domain::Session;
use crate::queries::{SessionQuery, SessionView, SimpleLoggingQuery};
use crate::services::{HappyPathSessionServices, SessionServices};

pub fn cqrs_framework(
    pool: Pool<Postgres>,
) -> (
    Arc<PostgresCqrs<Session>>,
    Arc<PostgresViewRepository<SessionView, Session>>,
) {
    // A very simple query that writes each event to stdout.
    let simple_query = SimpleLoggingQuery {};

    // A query that stores the current state of an individual account.
    let account_view_repo = Arc::new(PostgresViewRepository::new("session_query", pool.clone()));
    let mut account_query = SessionQuery::new(account_view_repo.clone());

    // Without a query error handler there will be no indication if an
    // error occurs (e.g., database connection failure, missing columns or table).
    // Consider logging an error or panicking in your own application.
    account_query.use_error_handler(Box::new(|e| println!("{}", e)));

    // Create and return an event-sourced `CqrsFramework`.
    let queries: Vec<Box<dyn Query<Session>>> =
        vec![Box::new(simple_query), Box::new(account_query)];
    let services = SessionServices::new(Box::new(HappyPathSessionServices));
    (
        Arc::new(postgres_es::postgres_cqrs(pool, queries, services)),
        account_view_repo,
    )
}