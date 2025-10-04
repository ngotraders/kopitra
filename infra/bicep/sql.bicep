// Review docs/standards.md before modifying this module. SQL resources must follow the naming,
// tagging, and security requirements defined in docs/standards.md.

@description('Azure region for the SQL logical server and database.')
param location string

@description('Logical server name.')
param serverName string

@description('Database name.')
param databaseName string

@description('Tag dictionary applied to SQL resources.')
param tags object

@description('Administrator login for the SQL logical server.')
param administratorLogin string

@description('Administrator password for the SQL logical server.')
@secure()
param administratorPassword string

resource sqlServer 'Microsoft.Sql/servers@2022-02-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-02-01-preview' = {
  name: '${sqlServer.name}/${databaseName}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: 'Local'
  }
}

output serverId string = sqlServer.id
output databaseId string = sqlDatabase.id
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
