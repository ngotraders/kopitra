// Review docs/standards.md before modifying this module. The template enforces naming, tagging,
// and managed identity requirements described in the standards document.

@description('Target environment suffix. Must be one of the sanctioned values defined in docs/standards.md.')
@allowed([
  'dev'
  'stg'
  'prod'
])
param environment string

@description('Short workload identifier used for resource naming and tagging.')
param workloadName string = 'kopitra'

@description('Azure region for the Container App resource.')
param location string = resourceGroup().location

@description('Resource ID of the existing Container Apps managed environment.')
param containerAppEnvironmentId string

@description('Container image reference (e.g., registry.azurecr.io/image:tag).')
param containerImage string

@description('Optional environment variables for the container. Each item must include a name and either a value or secretRef.')
param containerEnvVars array = []

@description('UPN or distribution list responsible for the application. Required tag per docs/standards.md.')
param ownerContact string

@description('Finance charge code used for the costCenter tag (e.g., FIN-1234).')
param costCenter string

@description('Data classification value used for the dataClassification tag.')
@allowed([
  'Public'
  'Internal'
  'Confidential'
  'Restricted'
])
param dataClassification string

@description('Additional tags to merge with the required base tags defined in docs/standards.md.')
param additionalTags object = {}

var containerAppName = '${workloadName}-aca-${environment}'
var baseTags = {
  application: workloadName
  environment: environment
  owner: ownerContact
  costCenter: costCenter
  dataClassification: dataClassification
}
var tags = union(additionalTags, baseTags)

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  identity: {
    // System-assigned identity satisfies the managed identity guidance in docs/standards.md.
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {
      ingress: {
        external: false
        targetPort: 80
      }
      secrets: []
    }
    template: {
      containers: [
        {
          name: '${workloadName}-api'
          image: containerImage
          env: containerEnvVars
          resources: {
            cpu: 0.5
            memory: '1.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output containerAppName string = containerApp.name
