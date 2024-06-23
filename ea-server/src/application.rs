use crate::domain::{Session, SessionCommand, SessionError};
use crate::queries::SessionView;
use crate::{config::cqrs_framework, persist::InMemoryViewRepository};
use cqrs_es::persist::{PersistenceError, ViewRepository};
use cqrs_es::AggregateError;
use cqrs_es::{mem_store::MemStore, CqrsFramework};
use std::collections::HashMap;
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
        let mut metadata = HashMap::new();
        metadata.insert("ea_version".to_string(), ea_version.to_string());
        self.cqrs
            .execute_with_metadata(
                &aggregate_id,
                SessionCommand::OpenSession {
                    ea_key: ea_key.to_string(),
                },
                metadata,
            )
            .await?;
        Ok(aggregate_id)
    }

    pub async fn close_session(
        &self,
        auth_token: &str,
        ea_key: &str,
        ea_version: &str,
    ) -> Result<(), AggregateError<SessionError>> {
        let mut metadata = HashMap::new();
        metadata.insert("ea_version".to_string(), ea_version.to_string());
        self.cqrs
            .execute_with_metadata(
                auth_token,
                SessionCommand::CloseSession {
                    ea_key: ea_key.to_string(),
                },
                metadata,
            )
            .await?;
        Ok(())
    }

    pub async fn find_session_view(
        &self,
        auth_token: &str,
    ) -> Result<Option<SessionView>, PersistenceError> {
        let view = self.session_query.load(auth_token).await?;
        Ok(view)
    }
}

#[cfg(test)]
mod test {
    use crate::application::ApplicationState;

    #[tokio::test]
    async fn test_open_session() {
        let state = &ApplicationState::new();
        let session_id = state
            .open_session("ea-key", "ea-version")
            .await
            .expect("State should return session id");
        assert!(!session_id.is_empty());
        if let Some(view) = state
            .find_session_view(&session_id)
            .await
            .expect("Error on find_session_view")
        {
            assert!(session_id==view.id);
            assert!("ea-key"==view.ea_key);
            assert!("ea-version"==view.ea_version.expect("Err"));
            assert!(view.open);
        } else {
            panic!("failed");
        }
    }

    #[tokio::test]
    async fn test_close_session() {
        let state = ApplicationState::new();
        let session_id = state
            .open_session("ea-key", "ea-version")
            .await
            .expect("State should return session id");
        state
            .close_session(&session_id, "ea-key", "ea-version")
            .await
            .expect("State should return session id");
        assert!(!session_id.is_empty());
        if let Some(view) = state
            .find_session_view(&session_id)
            .await
            .expect("Error on find_session_view")
        {
            assert!(session_id==view.id);
            assert!("ea-key"==view.ea_key);
            assert!("ea-version"==view.ea_version.expect("Err"));
            assert!(!view.open);
        } else {
            panic!("failed");
        }
    }
}
