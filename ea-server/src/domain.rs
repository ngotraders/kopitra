use std::fmt::{Display, Formatter};

use async_trait::async_trait;
use cqrs_es::{Aggregate, DomainEvent};
use serde::{Deserialize, Serialize};

use crate::services::SessionServices;

#[derive(Debug, Deserialize)]
pub enum SessionCommand {
    OpenSession { ea_key: String, ea_version: String },
    CloseSession {},
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum SessionEvent {
    SessionOpened { ea_key: String, ea_version: String },
    SessionClosed {},
}

impl DomainEvent for SessionEvent {
    fn event_type(&self) -> String {
        let event_type: &str = match self {
            SessionEvent::SessionOpened { .. } => "SessionOpened",
            SessionEvent::SessionClosed { .. } => "SessionClosed",
        };
        event_type.to_string()
    }

    fn event_version(&self) -> String {
        "1.0".to_string()
    }
}

#[derive(Debug)]
pub struct SessionError(String);

impl Display for SessionError {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.0)
    }
}

impl std::error::Error for SessionError {}

impl From<&str> for SessionError {
    fn from(message: &str) -> Self {
        SessionError(message.to_string())
    }
}

#[derive(Serialize, Default, Deserialize)]
pub struct Session {
    opened: bool,
}

#[async_trait]
impl Aggregate for Session {
    type Command = SessionCommand;
    type Event = SessionEvent;
    type Error = SessionError;
    type Services = SessionServices;

    // This identifier should be unique to the system.
    fn aggregate_type() -> String {
        "Session".to_string()
    }

    // The aggregate logic goes here. Note that this will be the _bulk_ of a CQRS system
    // so expect to use helper functions elsewhere to keep the code clean.
    async fn handle(
        &self,
        command: Self::Command,
        _services: &Self::Services,
    ) -> Result<Vec<Self::Event>, Self::Error> {
        match command {
            SessionCommand::OpenSession { ea_key, ea_version } => {
                return Ok(vec![SessionEvent::SessionOpened { ea_key, ea_version }])
            }
            SessionCommand::CloseSession {} => return Ok(vec![SessionEvent::SessionClosed {}]),
        }
    }

    fn apply(&mut self, event: Self::Event) {
        match event {
            SessionEvent::SessionOpened { .. } => self.opened = true,
            SessionEvent::SessionClosed { .. } => self.opened = false,
        }
    }
}

#[cfg(test)]
mod test {
    use crate::services::{HappyPathSessionServices, SessionServices};

    use super::{Session, SessionCommand, SessionEvent};
    use cqrs_es::test::TestFramework;
    type SessionTestFramework = TestFramework<Session>;

    #[test]
    fn test_close() {
        let previous = SessionEvent::SessionOpened {
            ea_key: "acc-key".to_string(),
            ea_version: "version".to_string(),
        };
        let expected = SessionEvent::SessionClosed {};

        SessionTestFramework::with(SessionServices::new(Box::new(HappyPathSessionServices {})))
            .given(vec![previous])
            .when(SessionCommand::CloseSession {})
            .then_expect_events(vec![expected]);
    }
}
