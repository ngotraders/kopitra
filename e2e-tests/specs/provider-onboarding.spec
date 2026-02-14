Specification: Provider onboarding

# Provider onboarding

## Scenario: Provider registers and reaches dashboard
* Given the backend API is reachable
* And the frontend login page is accessible
* When I create a temporary provider account with password GaugePass123!
* And I log in through the UI using that account
* Then I should see the dashboard navigation
