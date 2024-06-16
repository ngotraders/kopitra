use async_trait::async_trait;

pub struct SessionServices {
    pub services: Box<dyn AccountApi>,
}

impl SessionServices {
    pub fn new(services: Box<dyn AccountApi>) -> Self {
        Self { services }
    }
}

#[async_trait]
pub trait AccountApi: Sync + Send {
    async fn open_session(&self, account_key: &str, session_id: &str) -> Result<(), AccountError>;
    async fn close_session(&self, account_key: &str, session_id: &str) -> Result<(), AccountError>;
}

pub struct AccountError;

// A very simple "happy path" set of services that always succeed.
pub struct HappyPathSessionServices;

#[async_trait]
impl AccountApi for HappyPathSessionServices {
    async fn open_session(
        &self,
        _account_key: &str,
        _session_id: &str,
    ) -> Result<(), AccountError> {
        Ok(())
    }

    async fn close_session(
        &self,
        _account_key: &str,
        _session_id: &str,
    ) -> Result<(), AccountError> {
        Ok(())
    }
}
