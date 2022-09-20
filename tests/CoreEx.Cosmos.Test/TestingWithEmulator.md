# About

Tests can be executed with [Cosmos Db Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/linux-emulator?tabs=sql-api%2Cssl-netstd21) running in a container.

## Docker compose

Use following docker compose file to simulate github actions environment:

```yaml
# To run in root directory: docker-compose -f docker-compose.cosmos.yml run --rm myhr-cosmos-tests
version: '3.4'

services:

  cosmos:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    container_name: azure-cosmos-emulator
    tty: true
    ports:
      - 8081:8081
      - 10251:10251
      - 10252:10252
      - 10253:10253
      - 10254:10254
      - 10255:10255
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=false
    # cpus: "2.0" # Use this param at v2
    # mem_limit: 3g # Use this param at v2
    deploy: # Use these param at v3 & add `–compatibility` when compose up
      resources:
        limits:
          cpus: "2.0"
          memory: 3g

  myhr-cosmos-tests:
    image: mcr.microsoft.com/dotnet/sdk:6.0
    stdin_open: true # docker run -i
    tty: true        # docker run -t
    volumes:
       - .:/src    

    depends_on:
      - cosmos
```

## Running tests

* Connection string needs to be updated to reflect docker service name: `AzCosmos.CosmosClient("https://cosmos:8081"`.
* cd to tests directory `cd src/tests/CoreEx.Cosmos.Test/`
* run tests `dotnet test`
