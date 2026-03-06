@description('The name of the Static Web App.')
param name string

@description('The Azure region to deploy to.')
param location string = resourceGroup().location

@description('The SKU of the Static Web App. Free does not support custom auth.')
@allowed(['Free', 'Standard'])
param sku string = 'Standard'

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: name
  location: location
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

output defaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name

// ~END~
