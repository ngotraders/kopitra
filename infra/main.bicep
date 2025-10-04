// Review docs/standards.md before modifying this template. Every resource must follow the naming,
// tagging, and managed identity requirements documented there.

@description('Target environment suffix. Must match the allowed values in docs/standards.md.')
@allowed([
  'dev'
  'stg'
  'prod'
])
param environment string

@description('Azure region for the deployment. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short workload identifier used for naming and tagging.')
param workloadName string = 'kopitra'

@description('Owner contact (UPN or distribution list) applied to the owner tag.')
param ownerContact string

@description('Finance charge code applied to the costCenter tag.')
param costCenter string

@description('Data classification applied to the dataClassification tag.')
@allowed([
  'Public'
  'Internal'
  'Confidential'
  'Restricted'
])
param dataClassification string

@description('SQL administrator login used when creating the Azure SQL logical server.')
param sqlAdministratorLogin string

@description('SQL administrator password used for the logical server. Stored securely in the control plane only.')
@secure()
param sqlAdministratorPassword string

@description('Initial container image used for the gateway Container App. The GitHub workflow updates the image after building the service.')
param gatewayImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Set to true to provision the Cosmos DB account and container.')
param deployCosmos bool = false

var tags = {
  application: workloadName
  environment: environment
  owner: ownerContact
  costCenter: costCenter
  dataClassification: dataClassification
}

var resourceGroupName = resourceGroup().name
var logAnalyticsName = '${workloadName}-law-${environment}'
var managedEnvironmentName = '${workloadName}-cae-${environment}'
var gatewayAppName = '${workloadName}-gateway-aca-${environment}'
var containerRegistryName = toLower('${workloadName}${environment}acr')
var functionAppPlanName = '${workloadName}-plan-${environment}'
var functionAppName = '${workloadName}-func-${environment}'
var storageAccountName = toLower('${workloadName}${environment}st')
var serviceBusNamespaceName = '${workloadName}-sb-${environment}'
var operationsQueueName = 'tradeagent-operations-${environment}'
var commandsQueueName = 'tradeagent-commands-${environment}'
var sqlServerName = '${workloadName}-sql-${environment}'
var sqlDatabaseName = '${workloadName}-db-${environment}'
var keyVaultName = toLower('${workloadName}-kv-${environment}')
var staticWebAppName = '${workloadName}-ops-${environment}'
var appInsightsName = '${workloadName}-appi-${environment}'
var cosmosAccountName = toLower('${workloadName}-cdb-${environment}')
var cosmosDatabaseName = '${workloadName}-cosmos-${environment}'
var cosmosContainerName = 'events'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

module containerAppModule 'bicep/containerapps.bicep' = {
  name: 'containerApps'
  params: {
    location: location
    tags: tags
    workloadName: workloadName
    logAnalyticsName: logAnalyticsName
    managedEnvironmentName: managedEnvironmentName
    containerAppName: gatewayAppName
    containerImage: gatewayImage
    containerEnvVars: [
      {
        name: 'SERVICEBUS_NAMESPACE'
        value: '${serviceBusNamespaceName}.servicebus.windows.net'
      }
      {
        name: 'SERVICEBUS_OPERATIONS_QUEUE'
        value: operationsQueueName
      }
      {
        name: 'SERVICEBUS_COMMANDS_QUEUE'
        value: commandsQueueName
      }
      {
        name: 'SQL_SERVER_FQDN'
        value: '${sqlServerName}.${az.environment().suffixes.sqlServerHostname}'
      }
      {
        name: 'SQL_DATABASE_NAME'
        value: sqlDatabaseName
      }
      {
        name: 'KEY_VAULT_URI'
        value: 'https://${keyVaultName}.${az.environment().suffixes.keyVaultDns}/'
      }
    ]
    registryServer: containerRegistry.properties.loginServer
  }
}

module serviceBusModule 'bicep/servicebus.bicep' = {
  name: 'serviceBus'
  params: {
    namespaceName: serviceBusNamespaceName
    location: location
    tags: tags
    operationsQueueName: operationsQueueName
    commandsQueueName: commandsQueueName
  }
}

module sqlModule 'bicep/sql.bicep' = {
  name: 'sql'
  params: {
    serverName: sqlServerName
    databaseName: sqlDatabaseName
    location: location
    tags: tags
    administratorLogin: sqlAdministratorLogin
    administratorPassword: sqlAdministratorPassword
  }
}

module keyVaultModule 'bicep/keyvault.bicep' = {
  name: 'keyVault'
  params: {
    keyVaultName: keyVaultName
    location: location
    tags: tags
    tenantId: subscription().tenantId
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusNamespaceName
}

resource keyVaultResource 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

module cosmosModule 'bicep/cosmosdb.bicep' = if (deployCosmos) {
  name: 'cosmosDb'
  params: {
    accountName: cosmosAccountName
    location: location
    tags: tags
    databaseName: cosmosDatabaseName
    containerName: cosmosContainerName
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    IngestionMode: 'LogAnalytics'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  tags: tags
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource functionPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: functionAppPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  tags: tags
}

var storageAccountKeys = storageAccount.listKeys()
var storageAccountKey = storageAccountKeys.keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccountKey};EndpointSuffix=${az.environment().suffixes.storage}'
var sqlServerHostSuffix = az.environment().suffixes.sqlServerHostname
var keyVaultDnsSuffix = az.environment().suffixes.keyVaultDns
var keyVaultUri = 'https://${keyVaultName}.${keyVaultDnsSuffix}/'

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: functionPlan.id
    reserved: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'SERVICEBUS_NAMESPACE'
          value: '${serviceBusNamespaceName}.servicebus.windows.net'
        }
        {
          name: 'SERVICEBUS_OPERATIONS_QUEUE'
          value: operationsQueueName
        }
        {
          name: 'SERVICEBUS_COMMANDS_QUEUE'
          value: commandsQueueName
        }
        {
          name: 'SQL_SERVER_FQDN'
          value: '${sqlServerName}.${sqlServerHostSuffix}'
        }
        {
          name: 'SQL_DATABASE_NAME'
          value: sqlDatabaseName
        }
        {
          name: 'KEY_VAULT_URI'
          value: keyVaultUri
        }
      ]
    }
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2022-03-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  tags: tags
  properties: {
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-02-01-preview' = {
  name: '${sqlServerName}/AllowAzureServices'
  dependsOn: [
    sqlModule
  ]
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, containerAppModule.outputs.containerAppPrincipalId, 'AcrPull')
  scope: containerRegistry
  properties: {
    principalId: containerAppModule.outputs.containerAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource serviceBusSenderAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, serviceBusNamespaceName, gatewayAppName, 'sb-sender')
  scope: serviceBusNamespace
  properties: {
    principalId: containerAppModule.outputs.containerAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource serviceBusReceiverAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, serviceBusNamespaceName, functionAppName, 'sb-receiver')
  scope: serviceBusNamespace
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
  }
}

resource serviceBusSenderAssignmentFunctions 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, serviceBusNamespaceName, functionAppName, 'sb-sender-func')
  scope: serviceBusNamespace
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
  }
}

resource keyVaultContainerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, keyVaultName, gatewayAppName, 'kv-reader')
  scope: keyVaultResource
  properties: {
    principalId: containerAppModule.outputs.containerAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

resource keyVaultFunctionAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, keyVaultName, functionAppName, 'kv-reader-func')
  scope: keyVaultResource
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

output resourceGroupName string = resourceGroupName
output containerAppName string = gatewayAppName
output containerAppEnvironmentId string = containerAppModule.outputs.managedEnvironmentId
output containerRegistryLoginServer string = '${containerRegistry.name}.azurecr.io'
output serviceBusNamespace string = '${serviceBusNamespaceName}.servicebus.windows.net'
output operationsQueue string = operationsQueueName
output commandsQueue string = commandsQueueName
output sqlServerFqdn string = '${sqlServerName}.${sqlServerHostSuffix}'
output sqlDatabaseName string = sqlDatabaseName
output functionAppName string = functionAppName
output staticWebAppName string = staticWebAppName
output keyVaultUri string = keyVaultUri
output cosmosAccountName string = deployCosmos ? cosmosAccountName : ''
