use std::sync::Arc;

use async_trait::async_trait;
use eventually::{aggregate, command, message};

use crate::domain;

#[derive(Clone)]
pub struct Service {
    repository: Arc<dyn aggregate::Repository<domain::Account>>,
}

impl<R> From<R> for Service
where
    R: aggregate::Repository<domain::Account> + 'static,
{
    fn from(repository: R) -> Self {
        Self {
            repository: Arc::new(repository),
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct CreateAccount {
    pub account_id: domain::AccountId,
    pub account_key: domain::AccountKey,
}

impl message::Message for CreateAccount {
    fn name(&self) -> &'static str {
        "CreateAccount"
    }
}

#[async_trait]
impl command::Handler<CreateAccount> for Service {
    type Error = anyhow::Error;

    async fn handle(&self, command: command::Envelope<CreateAccount>) -> Result<(), Self::Error> {
        let command = command.message;

        let mut account = domain::AccountRoot::new(command.account_id, command.account_key)?;

        self.repository.save(&mut account).await?;

        Ok(())
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ConnectAccount {
    pub account_id: domain::AccountId,
    pub account_key: domain::AccountKey,
    pub ea_version: domain::EaVersion,
}

impl message::Message for ConnectAccount {
    fn name(&self) -> &'static str {
        "ConnectAccount"
    }
}

#[async_trait]
impl command::Handler<ConnectAccount> for Service {
    type Error = anyhow::Error;

    async fn handle(&self, command: command::Envelope<ConnectAccount>) -> Result<(), Self::Error> {
        let command = command.message;
        let mut account_root: domain::AccountRoot =
            self.repository.get(&command.account_id).await?.into();
        account_root.connect(command.account_key, command.ea_version)?;
        self.repository.save(&mut account_root).await?;

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    use crate::domain::AccountRepository;
    use eventually::{command, event};
    use tokio;

    #[tokio::test]
    async fn test_create_account() {
        command::test::Scenario
            .when(
                CreateAccount {
                    account_id: "account-test".to_owned(),
                    account_key: "account_key".to_owned(),
                }
                .into(),
            )
            .then(vec![event::Persisted {
                stream_id: "account-test".to_owned(),
                version: 1,
                event: domain::AccountEvent::Created {
                    id: "account-test".to_owned(),
                    key: "account_key".to_owned(),
                }
                .into(),
            }])
            .assert_on(|event_store| Service::from(AccountRepository::from(event_store)))
            .await;
    }

    #[tokio::test]
    async fn test_connect_account() {
        command::test::Scenario
            .given(vec![event::Persisted {
                stream_id: "account-test".to_owned(),
                version: 1,
                event: domain::AccountEvent::Created {
                    id: "account-test".to_owned(),
                    key: "account_key".to_owned(),
                }
                .into(),
            }])
            .when(
                ConnectAccount {
                    account_id: "account-test".to_owned(),
                    account_key: "account_key".to_owned(),
                    ea_version: "20240510".to_owned(),
                }
                .into(),
            )
            .then(vec![
                event::Persisted {
                    stream_id: "account-test".to_owned(),
                    version: 2,
                    event: domain::AccountEvent::Connected {
                        id: "account-test".to_owned(),
                        ea_version: "20240510".to_owned(),
                    }
                    .into(),
                },
            ])
            .assert_on(|event_store| Service::from(AccountRepository::from(event_store)))
            .await;
    }
}
