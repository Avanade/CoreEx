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
```

## Services

Available services:
* Database at port 5433
* API at port 5103
* Functions at 5104

Sample curl commands:
```
curl localhost:5104/api/health
curl localhost:5104/api/health  # to [get] to 'HealthInfo'
curl localhost:5104/api/employee/verify  # to [post] to 'HttpTriggerQueueVerificationFuncion'
curl localhost:5104/api/oauth2-redirect.html  # to [GET] to 'RenderOAuth2Redirect'
curl localhost:5104/api/openapi/{version}.{extension}  # to [GET] to 'RenderOpenApiDocument'
curl localhost:5104/api/swagger.{extension}  # to [GET] to 'RenderSwaggerDocument'
curl localhost:5104/api/swagger/ui  # to [GET] to 'RenderSwaggerUI'