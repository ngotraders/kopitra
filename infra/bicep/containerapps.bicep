// Review docs/standards.md before modifying this module. The template enforces naming, tagging,
// and managed identity requirements described in the standards document.

@description('Azure region for the Container Apps resources.')
param location string

@description('Tag dictionary merged into all created resources.')
param tags object

@description('Short workload identifier used for naming child resources and diagnostics.')
param workloadName string

@description('Name assigned to the Log Analytics workspace that backs the Container Apps environment.')
param logAnalyticsName string

@description('Name for the managed Container Apps environment.')
param managedEnvironmentName string

@description('Container App resource name.')
param containerAppName string

@description('Initial container image reference (e.g., registry.azurecr.io/image:tag).')
param containerImage string

@description('Optional environment variables to inject into the container.')
param containerEnvVars array = []

@description('Target ingress port exposed by the application.')
param targetPort int = 8080

@description('CPU cores allocated to the container. Provide the value as a string that can be parsed as JSON (e.g. "0.25").')
param cpu string = '0.25'

@description('Memory allocated to the container.')
param memory string = '0.5Gi'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: managedEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listkeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
      }
    }
    template: {
      containers: [
        {
          name: '${workloadName}-gateway'
          image: containerImage
          env: containerEnvVars
          resources: {
            cpu: json(cpu)
            memory: memory
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output managedEnvironmentId string = managedEnvironment.id
output containerAppId string = containerApp.id
output logAnalyticsId string = logAnalytics.id
