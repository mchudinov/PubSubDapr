param location string
param environmentName string
param publisherAppName string
param subscriberAppName string
param publisherImage string
param subscriberImage string
param serviceBusConnectionString string
param redisHostname string
param redisPassword string

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${environmentName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container Apps Environment
resource cae 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: listKeys(logAnalytics.id, logAnalytics.apiVersion).primarySharedKey
      }
    }
  }
}

// Dapr Component: pubsub (Azure Service Bus)
resource pubsubComponent 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  parent: cae
  name: 'servicebus-pubsub'
  properties: {
    componentType: 'pubsub.azure.servicebus'
    version: 'v1'
    metadata: [
      {
        name: 'connectionString'
        value: serviceBusConnectionString
      }
      {
        name: 'consumerID'
        value: 'sbs-subscription1'
      }
      {
        name: 'disableEntityManagement'
        value: 'true'
      }
    ]
    scopes: [
      'pub-app-id'
      'sub-app-id'
    ]
  }
}

// Dapr Component: statestore (Redis)
resource statestoreComponent 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  parent: cae
  name: 'statestore'
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHostname}:6380'
      }
      {
        name: 'redisPassword'
        value: redisPassword
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
    scopes: [
      'sub-app-id'
    ]
  }
}

// Publisher Container App
resource publisherApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: publisherAppName
  location: location
  properties: {
    environmentId: cae.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8082
      }
      dapr: {
        enabled: true
        appId: 'pub-app-id'
        appPort: 8082
        appProtocol: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'publisher'
          image: publisherImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8082'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [pubsubComponent]
}

// Subscriber Container App
resource subscriberApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: subscriberAppName
  location: location
  properties: {
    environmentId: cae.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8083
      }
      dapr: {
        enabled: true
        appId: 'sub-app-id'
        appPort: 8083
        appProtocol: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'subscriber'
          image: subscriberImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8083'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [pubsubComponent, statestoreComponent]
}

output publisherFqdn string = publisherApp.properties.configuration.ingress.fqdn
output subscriberFqdn string = subscriberApp.properties.configuration.ingress.fqdn
