# About

To run with docker-compose:

```bash
docker-compose -f docker-compose.myHr.yml -f docker-compose.myHr.override.yml -f docker-compose.local.override.yml up
```

where `docker-compose.local.override.yml` should include connection string to service bus:

```yaml
version: '3.4'

services:
  myhr-functions:
    environment:
      - ServiceBusConnection=Endpoint=sb://coreex.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxx

  myhr-api:
    environment:
      - ServiceBusConnection=Endpoint=sb://coreex.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxx
```

Service Bus should have `pendingverifications` queue used by *My.Hr* sample.

## Running SQL database only

It's possible to run only DB container for local development with

```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.DB.only.yml up
```

## To build

```bash
docker-compose -f docker-compose.myHr.yml -f docker-compose.myHr.override.yml -f docker-compose.local.override.yml build --build-arg LOCAL=true
```

## Services

Available services:

* Database at port 5433
* API at port 5103
* Functions at 5104

Sample curl commands:

### Function

```bash
curl localhost:5104/api/health  # to [get] to 'HealthInfo'
curl localhost:5104/api/employee/verify  # to [post] to 'HttpTriggerQueueVerificationFunction'
curl localhost:5104/api/oauth2-redirect.html  # to [GET] to 'OAuth2Redirect'
curl localhost:5104/api/openapi/{version}.{extension}  # to [GET] to 'OpenApiDocument'
curl localhost:5104/api/swagger.{extension}  # to [GET] to 'SwaggerDocument'
curl localhost:5104/api/swagger/ui  # to [GET] to 'SwaggerUI'
```

### API

```bash
curl localhost:5103/health  # to [get] to 'HealthInfo' 
curl localhost:5103/swagger/index.html #  to [GET] to 'SwaggerUI'
```
