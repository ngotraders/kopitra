// Review docs/standards.md before modifying this module. Key Vault deployments must preserve the
// naming, tagging, and RBAC guidance documented there.

@description('Azure region for the Key Vault.')
param location string

@description('Key Vault resource name.')
param keyVaultName string

@description('Tag dictionary applied to the vault.')
param tags object

@description('Azure AD tenant ID for the Key Vault.')
param tenantId string

@description('Set to true to enable purge protection on the vault.')
param enablePurgeProtection bool = true

@description('Soft-delete retention in days.')
param softDeleteRetentionDays int = 90

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    softDeleteRetentionInDays: softDeleteRetentionDays
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: 'Enabled'
  }
}

output vaultId string = keyVault.id
output vaultUri string = keyVault.properties.vaultUri
