targetScope = 'subscription'

@description('Application name used as suffix in resource names')
param appName string = 'pubsubdapr'

@description('Azure region for all resources')
param location string

@description('Container image for the Publisher app')
param publisherImage string

@description('Container image for the Subscriber app')
param subscriberImage string

resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${appName}'
  location: location
}

module resources 'modules/resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    appName: appName
    location: location
    publisherImage: publisherImage
    subscriberImage: subscriberImage
  }
}

output resourceGroup string = rg.name
output publisherUrl string = resources.outputs.publisherUrl
output subscriberUrl string = resources.outputs.subscriberUrl
output serviceBusNamespace string = resources.outputs.serviceBusNamespace
