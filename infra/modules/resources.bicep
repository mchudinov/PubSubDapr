param appName string
param location string
param publisherImage string
param subscriberImage string

var serviceBusName = 'sb-${appName}'
var redisName = 'redis-${appName}'
var caeEnvironmentName = 'cae-${appName}'
var publisherAppName = 'aca-pub'
var subscriberAppName = 'aca-sub'

module servicebus 'servicebus.bicep' = {
  name: 'servicebus'
  params: {
    namespaceName: serviceBusName
    location: location
  }
}

module redis 'redis.bicep' = {
  name: 'redis'
  params: {
    cacheName: redisName
    location: location
  }
}

module containerapps 'containerapps.bicep' = {
  name: 'containerapps'
  params: {
    location: location
    environmentName: caeEnvironmentName
    publisherAppName: publisherAppName
    subscriberAppName: subscriberAppName
    publisherImage: publisherImage
    subscriberImage: subscriberImage
    serviceBusConnectionString: servicebus.outputs.connectionString
    redisHostname: redis.outputs.hostname
    redisPassword: redis.outputs.primaryKey
  }
}

output publisherUrl string = 'https://${containerapps.outputs.publisherFqdn}'
output subscriberUrl string = 'https://${containerapps.outputs.subscriberFqdn}'
output serviceBusNamespace string = servicebus.outputs.namespaceName
