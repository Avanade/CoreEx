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

Todo:
add script: https://github.com/karpikpl/tests-with-docker-compose/blob/2158e44403d3659a2efe03d4af09b48756f46b76/Dockerfile