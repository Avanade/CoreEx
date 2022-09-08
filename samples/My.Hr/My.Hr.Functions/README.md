# About

tbd

## Configuration

Sample configuration for `local.settings.json`

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",

        "AgifyApiEndpointUri": "https://api.agify.io",
        "NationalizeApiClientApiEndpointUri": "https://api.nationalize.io",
        "GenderizeApiClientApiEndpointUri": "https://api.genderize.io",

        "VerificationQueueName": "pendingVerifications",
        "VerificationResultsQueueName": "verificationResults",

        "ServiceBusConnection__fullyQualifiedNamespace": "coreex.servicebus.windows.net",

        "HttpLogContent": "true",
        "AzureFunctionsJobHost__logging__logLevel__CoreEx": "Debug",
        "AzureFunctionsJobHost__logging__logToConsole": "true",
        "AzureFunctionsJobHost__logging__logToConsoleColor": "true",
        "AzureFunctionsJobHost__logging__console__isEnabled": "true",

        "MassPublishQueueName": "mass-publish"
    }
}
```
