param cacheName string
param location string

resource redisCache 'Microsoft.Cache/redis@2023-04-01' = {
  name: cacheName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

output hostname string = redisCache.properties.hostName
output primaryKey string = listKeys(redisCache.id, redisCache.apiVersion).primaryKey
