param location string
param namespaceName string
param skuName string
param tags object = {}

resource ns 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    minimumTlsVersion: '1.2'
  }
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: ns
  name: 'contoso'
  properties: {
    defaultMessageTimeToLive: 'P14D'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
  }
}

resource productsSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview' = {
  parent: topic
  name: 'products'
  properties: {
    requiresSession: true
    maxDeliveryCount: 10
    lockDuration: 'PT5M'
    deadLetteringOnMessageExpiration: true
  }
}

resource shoppingSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2023-01-01-preview' = {
  parent: topic
  name: 'shopping'
  properties: {
    requiresSession: true
    maxDeliveryCount: 10
    lockDuration: 'PT5M'
    deadLetteringOnMessageExpiration: true
  }
}

resource authRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2023-01-01-preview' = {
  parent: ns
  name: 'app'
  properties: {
    rights: [
      'Listen'
      'Send'
      'Manage'
    ]
  }
}

output namespaceName string = ns.name
output id string = ns.id
output topicName string = topic.name
output productsSubscriptionName string = productsSubscription.name
output shoppingSubscriptionName string = shoppingSubscription.name
output connectionString string = listKeys(authRule.id, authRule.apiVersion).primaryConnectionString
