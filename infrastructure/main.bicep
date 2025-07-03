@description('The name of the App Service plan (base name, will have environment appended)')
param appServicePlanBaseName string = 'intelligencehub-appserviceplan'

@description('The name of the Web App. Similarly, the environment is appended later.')
param WebAppBaseName string = 'IntelligenceHub'

@description('The location for all resources.')
param location string = 'Canada Central'

@description('The environment (e.g., dev, prod)')
param environment string = 'dev'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${appServicePlanBaseName}-${environment}'
  location: location
  sku: {
    name: 'F1' // Free tier for dev/test
    tier: 'Free'
  }
  properties: {
    reserved: false
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: '${WebAppBaseName}-${environment}'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
  }
  tags: {
    environment: environment
  }
}
