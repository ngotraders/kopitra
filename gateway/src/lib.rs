/// A simple representation of the gateway service health.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct HealthStatus {
    /// Name of the component being checked.
    pub component: &'static str,
    /// Human-readable description of the current state.
    pub message: &'static str,
    /// Indicates whether the component is healthy.
    pub healthy: bool,
}

impl HealthStatus {
    /// Creates a successful health check response for the provided component.
    pub const fn ok(component: &'static str) -> Self {
        Self {
            component,
            message: "ok",
            healthy: true,
        }
    }
}

/// Returns the health status of the gateway service.
pub fn health_check() -> HealthStatus {
    HealthStatus::ok("gateway")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn health_check_returns_ok_status() {
        let status = health_check();

        assert_eq!(status, HealthStatus::ok("gateway"));
        assert!(status.healthy);
        assert_eq!(status.message, "ok");
    }
}
