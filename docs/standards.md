# Infrastructure Naming, Tagging, and Identity Standards

These standards govern every infrastructure asset in this repository (Bicep modules, Terraform modules, deployment scripts, and supporting automation). They ensure that resources are easy to identify across environments, consistently tagged for governance, and securely connected to dependent data services.

## 1. Resource Naming Rules

### 1.1 Baseline Pattern

Use the following pattern for all Azure resources unless a service imposes a stricter naming convention:

```
<workload>-<resource>-<environment>
```

* **`<workload>`** – A short, unique identifier for the solution (e.g., `kopitra`).
* **`<resource>`** – A concise abbreviation for the Azure resource type.
* **`<environment>`** – The environment suffix. All resources **must** end in one of:
  * `dev`
  * `stg`
  * `prod`

The suffix must match the deployed environment. Do not share stateful resources across environments. Temporary or shared infrastructure should still include the suffix for the environment that owns the resource.

### 1.2 Recommended Abbreviations

| Resource Type                          | Abbreviation | Example Name            |
|---------------------------------------|--------------|-------------------------|
| Resource Group                        | `rg`         | `kopitra-rg-dev`        |
| Container Apps Environment            | `cae`        | `kopitra-cae-stg`       |
| Container App                         | `aca`        | `kopitra-aca-prod`      |
| Azure Container Registry              | `acr`        | `kopitra-acr-dev`       |
| Key Vault                             | `kv`         | `kopitra-kv-prod`       |
| Cosmos DB Account                     | `cdb`        | `kopitra-cdb-stg`       |
| SQL Logical Server                    | `sql`        | `kopitra-sql-prod`      |
| Service Bus Namespace                 | `sb`         | `kopitra-sb-dev`        |
| User-Assigned Managed Identity        | `id`         | `kopitra-id-prod`       |
| Storage Account                       | `st`         | `kopitrast-prod`        |

> **Note**: Storage accounts, Cosmos DB accounts, and other globally unique resources may require additional random characters. Append a short random suffix after the workload name: `kopitraxyz-cdb-prod`. Keep the suffix deterministic per environment to aid discovery.

### 1.3 Additional Naming Guidance

1. Use only lowercase letters, numbers, and hyphens unless the service imposes different constraints.
2. Keep names under service-specific length limits (most Azure resource names have a 63-character limit or less).
3. When child resources (e.g., Key Vault secrets, Service Bus topics, SQL databases) are environment-specific, extend the suffix into their names (e.g., `orders-api-connection-prod`).
4. Propagate naming conventions to DevOps pipelines, dashboards, and monitoring alerts so the environment can be identified immediately.

## 2. Tagging Standards

Tags drive governance, cost management, and operations. All Azure resources **must** include the following tag keys and values:

| Tag Key             | Required Value                                          | Purpose                                        |
|---------------------|---------------------------------------------------------|------------------------------------------------|
| `environment`       | `dev`, `stg`, or `prod` (must match the name suffix)    | Enables environment-specific policy targeting. |
| `application`       | Workload identifier (default `kopitra`)                 | Groups related resources for reporting.        |
| `owner`             | Azure AD UPN or distribution list for the owning team   | Provides an accountable contact.               |
| `costCenter`        | Finance charge code (e.g., `FIN-1234`)                  | Supports cost allocation.                      |
| `dataClassification`| `Public`, `Internal`, `Confidential`, or `Restricted`   | Aligns with data handling requirements.        |

Additional optional tags (e.g., `managedBy`, `lifecycle`, `supportTier`) are welcome, but do not replace the required set above.

### 2.1 Implementation Requirements

1. **Infrastructure as Code** – Modules must expose parameters for every required tag and apply them automatically. Required tag values must not be overridable by ad-hoc inputs.
2. **Resource Groups** – Apply tags at the resource group level so that inheriting resources get the same metadata. Validate that resources which do not inherit still receive the tags explicitly.
3. **Pipelines** – Continuous deployment pipelines should fail early if required tag parameters are missing. Include automated compliance checks (e.g., `az tag list`, Azure Policy) in the pipeline.
4. **Auditing** – Schedule recurring audits (Azure Policy, Azure Resource Graph queries) to report non-compliant resources.

## 3. Managed Identity Guidance for Container Apps

Azure Container Apps must use managed identities when accessing downstream data services. The following sections cover enabling the identity, granting access, and configuring applications for Cosmos DB, SQL Database, Service Bus, and Key Vault.

### 3.1 Enable a Managed Identity on the Container App

* **System-assigned identity (recommended for single apps)**
  * In Bicep: `identity: { type: 'SystemAssigned' }`.
  * Azure creates and rotates the identity lifecycle with the app. No additional management is required.
* **User-assigned identity (for shared access)**
  * Create a user-assigned managed identity (`Microsoft.ManagedIdentity/userAssignedIdentities`).
  * Reference it in the Container App definition: `identity: { type: 'UserAssigned', userAssignedIdentities: { <resourceId>: {} } }`.

Always remove unused user-assigned identities to prevent lingering permissions.

### 3.2 Assign Least-Privilege Roles

Create role assignments scoped as narrowly as possible (resource or sub-resource level). Typical role assignments include:

| Service        | Role(s) to Assign                                                                 | Scope Recommendation                             |
|----------------|------------------------------------------------------------------------------------|--------------------------------------------------|
| Cosmos DB      | `Cosmos DB Built-in Data Reader` or `Cosmos DB Built-in Data Contributor`          | Database or container level when RBAC-enabled.   |
| Azure SQL      | `Contributor` or `SQL DB Contributor` on the SQL server (Azure RBAC) then map the identity to a contained database user with `db_datareader`/`db_datawriter` roles. | Database level. |
| Service Bus    | `Azure Service Bus Data Sender`, `Azure Service Bus Data Receiver`, or `Azure Service Bus Data Owner` | Namespace, topic, or subscription as required.   |
| Key Vault      | `Key Vault Secrets User` (RBAC) or an access policy granting minimal secret/certificate permissions. | Vault or specific secret. |

Automate role assignments in infrastructure templates to guarantee deterministic access during deployments.

### 3.3 Configure Application Settings

1. Store service endpoints and database names (never access keys) as Container App secrets or environment variables.
2. Use Azure SDK DefaultAzureCredential/ManagedIdentityCredential in application code so the platform token is requested automatically.
3. For SQL Database, create a contained database user mapped to the managed identity and set the connection string to use `Authentication=Active Directory Default;`.
4. For Service Bus, configure the fully qualified namespace and rely on the managed identity for SAS token acquisition.
5. For Key Vault, reference secrets using the managed identity and disable fallback to client secrets.

### 3.4 Validate Access

* Include deployment-time smoke tests to verify the managed identity can read Cosmos DB documents, connect to SQL, send/receive Service Bus messages, and read Key Vault secrets.
* Monitor Azure AD sign-in logs for the managed identity to ensure token requests originate from expected resources.
* Document incident response steps for expired or revoked role assignments.

## 4. Using This Document

* Infrastructure templates **must** reference this file in comments and enforce the rules above (see `infra/container-app.bicep` for an example implementation).
* Application and operations teams should review these standards before provisioning new Azure resources or onboarding a new environment.
* Update this document whenever naming, tagging, or identity requirements evolve. Changes should be communicated to all teams via release notes or pull requests.
