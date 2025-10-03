// Review docs/standards.md before modifying this module. All Service Bus resources must align with
// the naming and tagging guidance in docs/standards.md.

@description('Azure region for the Service Bus namespace.')
param location string

@description('Namespace resource name.')
param namespaceName string

@description('Tag dictionary applied to all resources created by this module.')
param tags object

@description('Target environment suffix used for child resource names.')
param environment string

@description('Operations queue name. Must be unique within the namespace.')
param operationsQueueName string

@description('Commands queue name. Must be unique within the namespace.')
param commandsQueueName string

resource namespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
}

resource operationsQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: '${namespace.name}/${operationsQueueName}'
  properties: {
    lockDuration: 'PT30S'
    maxDeliveryCount: 5
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

resource commandsQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: '${namespace.name}/${commandsQueueName}'
  properties: {
    lockDuration: 'PT30S'
    maxDeliveryCount: 5
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P7D'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

output namespaceId string = namespace.id
output fullyQualifiedNamespace string = '${namespaceName}.servicebus.windows.net'
output operationsQueueNameOut string = operationsQueueName
output commandsQueueNameOut string = commandsQueueName
