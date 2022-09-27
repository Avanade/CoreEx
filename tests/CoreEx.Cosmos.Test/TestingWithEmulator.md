# About

Tests can be executed with [Cosmos Db Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/linux-emulator?tabs=sql-api%2Cssl-netstd21) running in a container.

> Caution! Emulator requires 4 CPUs to execute tests successfully. That's more than free GitHub runners offer (2).

## Docker compose

Use following docker compose file to simulate github actions environment:

```yaml
# To run in root directory: docker-compose -f docker-compose.cosmos.yml run --rm myhr-cosmos-tests
version: '3.4'

services:

  cosmos:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    container_name: azure-cosmos-emulator
    healthcheck:
      test: curl -f -k https://localhost:8081/_explorer/emulator.pem || exit 1
      interval: 1m30s
      timeout: 10s
      retries: 3
      start_period: 30s
    tty: true
    ports:
      - 8081:8081
      - 10251:10251
      - 10252:10252
      - 10253:10253
      - 10254:10254
      - 10255:10255
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=20
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=false
    deploy:
      resources:
        limits:
          cpus: "4.0" # Tests fail with 2 CPUs
          memory: 4g

  myhr-cosmos-tests:
    image: mcr.microsoft.com/dotnet/sdk:6.0
    stdin_open: true # docker run -i
    tty: true        # docker run -t
    volumes:
       - .:/src
    command: cd /src/tests/CoreEx.Cosmos.Test && dotnet test
    depends_on:
      - cosmos
```

## Running tests

* Connection string needs to be updated to reflect docker service name: `AzCosmos.CosmosClient("https://cosmos:8081"`.
* cd to tests directory `cd src/tests/CoreEx.Cosmos.Test/`
* run tests `dotnet test`
