use gateway::{HealthStatus, health_check};

#[test]
fn reports_gateway_component_health() {
    let status = health_check();

    assert_eq!(status, HealthStatus::ok("gateway"));
    assert!(status.healthy, "gateway health check should report healthy");
    assert_eq!(status.component, "gateway");
}
