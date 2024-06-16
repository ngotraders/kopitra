use crate::domain::{Session, SessionCommand, SessionError};
use crate::queries::SessionView;
use crate::{config::cqrs_framework, persist::InMemoryViewRepository};
use cqrs_es::AggregateError;
use cqrs_es::{mem_store::MemStore, CqrsFramework};
use std::sync::Arc;
use uuid::Uuid;

#[derive(Clone)]
pub struct ApplicationState {
    pub cqrs: Arc<CqrsFramework<Session, MemStore<Session>>>,
    pub session_query: Arc<InMemoryViewRepository<SessionView, Session>>,
}

impl ApplicationState {
    pub fn new() -> ApplicationState {
        let (cqrs, session_query) = cqrs_framework();
        ApplicationState {
            cqrs,
            session_query,
        }
    }

    pub async fn open_session(
        &self,
        ea_key: &str,
        ea_version: &str,
    ) -> Result<String, AggregateError<SessionError>> {
        let aggregate_id = Uuid::new_v4().to_string();
        self.cqrs
            .execute(
                &aggregate_id,
                SessionCommand::OpenSession {
                    ea_key: ea_key.to_string(),
                    ea_version: ea_version.to_string(),
                },
            )
            .await?;
        Ok(aggregate_id)
    }
}

#[cfg(test)]
mod test {
    use crate::application::ApplicationState;

    #[tokio::test]
    async fn test_open_session() {
        let state = ApplicationState::new();
        let session_id = state
            .open_session("ea-key", "ea-version")
            .await
            .expect("State should return session id");
        assert!(!session_id.is_empty())
    }
}
