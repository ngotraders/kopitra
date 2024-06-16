use async_trait::async_trait;

use serde::{Deserialize, Serialize};

use cqrs_es::{Aggregate, DomainEvent, EventEnvelope, View};

#[derive(Debug, Serialize, Deserialize, Default, Clone, PartialEq)]
pub struct TestAggregate {
    id: String,
    description: String,
    tests: Vec<String>,
}
#[derive(Debug, thiserror::Error)]
#[error("{0}")]
pub struct TestError(String);

impl From<&str> for TestError {
    fn from(msg: &str) -> Self {
        Self(msg.to_string())
    }
}

#[derive(Clone, Debug)]
pub struct TestService;

#[async_trait]
impl Aggregate for TestAggregate {
    type Command = TestCommand;
    type Event = TestEvent;
    type Error = TestError;
    type Services = TestService;

    fn aggregate_type() -> String {
        "TestAggregate".to_string()
    }

    async fn handle(
        &self,
        command: Self::Command,
        _service: &Self::Services,
    ) -> Result<Vec<TestEvent>, Self::Error> {
        match &command {
            TestCommand::CreateTest(command) => {
                let event = TestEvent::Created(Created {
                    id: command.id.to_string(),
                });
                Ok(vec![event])
            }

            TestCommand::ConfirmTest(command) => {
                for test in &self.tests {
                    if test == &command.test_name {
                        return Err("test already performed".into());
                    }
                }
                let event = TestEvent::Tested(Tested {
                    test_name: command.test_name.to_string(),
                });
                Ok(vec![event])
            }

            TestCommand::DoSomethingElse(command) => {
                let event = TestEvent::SomethingElse(SomethingElse {
                    description: command.description.clone(),
                });
                Ok(vec![event])
            }
        }
    }

    fn apply(&mut self, event: Self::Event) {
        match event {
            TestEvent::Created(e) => {
                self.id = e.id;
            }
            TestEvent::Tested(e) => {
                self.tests.push(e.test_name);
            }
            TestEvent::SomethingElse(e) => {
                self.description = e.description;
            }
        }
    }
}

#[derive(Debug, Serialize, Deserialize, Clone, PartialEq)]
pub enum TestEvent {
    Created(Created),
    Tested(Tested),
    SomethingElse(SomethingElse),
}

#[derive(Debug, Serialize, Deserialize, Clone, PartialEq)]
pub struct Created {
    pub id: String,
}

#[derive(Debug, Serialize, Deserialize, Clone, PartialEq)]
pub struct Tested {
    pub test_name: String,
}

#[derive(Debug, Serialize, Deserialize, Clone, PartialEq)]
pub struct SomethingElse {
    pub description: String,
}

impl DomainEvent for TestEvent {
    fn event_type(&self) -> String {
        match self {
            Self::Created(_) => "Created".to_string(),
            Self::Tested(_) => "Tested".to_string(),
            Self::SomethingElse(_) => "SomethingElse".to_string(),
        }
    }

    fn event_version(&self) -> String {
        "1.0".to_string()
    }
}

pub enum TestCommand {
    CreateTest(CreateTest),
    ConfirmTest(ConfirmTest),
    DoSomethingElse(DoSomethingElse),
}

pub struct CreateTest {
    pub id: String,
}

pub struct ConfirmTest {
    pub test_name: String,
}

pub struct DoSomethingElse {
    pub description: String,
}

#[derive(Debug, Default, Serialize, Deserialize, Clone, PartialEq)]
pub struct TestView {
    events: Vec<TestEvent>,
}

impl TestView {
    pub fn new(events: Vec<TestEvent>) -> Self {
        Self { events }
    }
}

impl View<TestAggregate> for TestView {
    fn update(&mut self, event: &EventEnvelope<TestAggregate>) {
        self.events.push(event.payload.clone());
    }
}
