// Review docs/standards.md before modifying this module. Cosmos DB resources must adhere to the
// naming and tagging requirements defined in docs/standards.md.

@description('Azure region for the Cosmos DB account.')
param location string

@description('Cosmos DB account name.')
param accountName string

@description('Tag dictionary applied to all Cosmos resources.')
param tags object

@description('SQL API database name.')
param databaseName string

@description('SQL API container name.')
param containerName string

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  tags: tags
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    publicNetworkAccess: 'Enabled'
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
        backupStorageRedundancy: 'LocallyRedundant'
      }
    }
  }
}

resource sqlDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  name: databaseName
  parent: cosmosAccount
  tags: tags
  properties: {
    resource: {
      id: databaseName
    }
    options: {}
  }
}

resource sqlContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: containerName
  parent: sqlDatabase
  tags: tags
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
      defaultTtl: -1
    }
    options: {}
  }
}

output accountId string = cosmosAccount.id
output databaseResourceId string = sqlDatabase.id
output containerResourceId string = sqlContainer.id
