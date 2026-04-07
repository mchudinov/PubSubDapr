param namespaceName string
param location string
param topicName string = 'sbt-topic1'
param subscriptionName string = 'sbs-subscription1'

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource sbTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: sbNamespace
  name: topicName
  properties: {
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P14D'
    requiresDuplicateDetection: false
    enableBatchedOperations: true
  }
}

resource sbSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: sbTopic
  name: subscriptionName
  properties: {
    maxDeliveryCount: 10
    lockDuration: 'PT1M'
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
  }
}

var primaryConnectionString = listKeys('${sbNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', sbNamespace.apiVersion).primaryConnectionString

output connectionString string = primaryConnectionString
output namespaceName string = sbNamespace.name
