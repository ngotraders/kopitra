use async_trait::async_trait;
use cqrs_es::persist::GenericQuery;
use cqrs_es::{EventEnvelope, Query, View};
use serde::{Deserialize, Serialize};

use crate::domain::Session;
use crate::domain::SessionEvent;
use crate::persist::InMemoryViewRepository;

pub struct SimpleLoggingQuery {}

// Our simplest query, this is great for debugging but absolutely useless in production.
// This query just pretty prints the events as they are processed.
#[async_trait]
impl Query<Session> for SimpleLoggingQuery {
    async fn dispatch(&self, aggregate_id: &str, events: &[EventEnvelope<Session>]) {
        for event in events {
            let payload = serde_json::to_string_pretty(&event.payload).unwrap();
            println!("{}-{}\n{}", aggregate_id, event.sequence, payload);
        }
    }
}

// Our second query, this one will be handled with Postgres `GenericQuery`
// which will serialize and persist our view after it is updated. It also
// provides a `load` method to deserialize the view on request.
pub type SessionQuery =
    GenericQuery<InMemoryViewRepository<SessionView, Session>, SessionView, Session>;

// The view for a Session query, for a standard http application this should
// be designed to reflect the response dto that will be returned to a user.
#[derive(Debug, Default, Serialize, Deserialize, Clone)]
pub struct SessionView {
    account_id: Option<String>,
    ea_key: String,
    ea_version: String,
    open: bool,
}

// This updates the view with events as they are committed.
// The logic should be minimal here, e.g., don't calculate the account balance,
// design the events to carry the balance information instead.
impl View<Session> for SessionView {
    fn update(&mut self, event: &EventEnvelope<Session>) {
        match &event.payload {
            SessionEvent::SessionOpened { ea_key, ea_version } => {
                self.open = true;
                self.ea_key = ea_key.clone();
                self.ea_version = ea_version.clone();
            }
            SessionEvent::SessionClosed { .. } => {
                self.open = false;
            }
        }
    }
}
