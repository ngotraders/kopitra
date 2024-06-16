use crate::{config::cqrs_framework, persist::InMemoryViewRepository};
use crate::domain::Session;
use crate::queries::SessionView;
use cqrs_es::{mem_store::MemStore, CqrsFramework};
use std::sync::Arc;

#[derive(Clone)]
pub struct ApplicationState {
    pub cqrs: Arc<CqrsFramework<Session, MemStore<Session>>>,
    pub session_query: Arc<InMemoryViewRepository<SessionView, Session>>,
}

pub async fn new_application_state() -> ApplicationState {
    // Configure the CQRS framework, backed by a Postgres database, along with two queries:
    // - a simply-query prints events to stdout as they are published
    // - `session_query` stores the current state of the account in a ViewRepository that we can access
    //
    // The needed database tables are automatically configured with `docker-compose up -d`,
    // see init file at `/db/init.sql` for more.
    let (cqrs, session_query) = cqrs_framework();
    ApplicationState {
        cqrs,
        session_query,
    }
}