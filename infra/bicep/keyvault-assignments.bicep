// Review docs/standards.md before modifying this module. Key Vault RBAC assignments
// must follow the identity and tagging requirements defined there.

targetScope = 'resourceGroup'

@description('Key Vault resource name that receives the role assignments.')
param keyVaultName string

@description('Fully qualified Key Vault URI used to establish deployment ordering.')
param keyVaultUri string

@description('Principal ID of the gateway Container App managed identity.')
param containerAppPrincipalId string

@description('Principal ID of the function app managed identity.')
param functionAppPrincipalId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource keyVaultContainerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, keyVaultName, keyVaultUri, containerAppPrincipalId, 'kv-reader')
  scope: keyVault
  properties: {
    principalId: containerAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource keyVaultFunctionAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, keyVaultName, keyVaultUri, functionAppPrincipalId, 'kv-reader-func')
  scope: keyVault
  properties: {
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}
