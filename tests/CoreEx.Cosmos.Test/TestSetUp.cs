using CoreEx.Cosmos.Batch;
using CoreEx.Json.Data;
using CoreEx.Mapping;
using AzCosmos = Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    public static class TestSetUp
    {
        public static AzCosmos.CosmosClient? CosmosClient { get; private set; }

        public static AzCosmos.Database? CosmosDatabase { get; private set; }

        public static IMapper? Mapper { get; private set; }

        public static async Task SetUpAsync(string partitionKeyPath = "/_partitionKey", string valuePartitionKeyPath = "/_partitionKey")
        {
            CoreEx.Cosmos.Batch.CosmosDbBatch.SequentialExecution = true;

            //cleanup if client was already created ??
            CosmosClient?.Dispose();

            var cco = new AzCosmos.CosmosClientOptions
            {
                SerializerOptions = new AzCosmos.CosmosSerializationOptions { PropertyNamingPolicy = AzCosmos.CosmosPropertyNamingPolicy.CamelCase, IgnoreNullValues = true },
                // https://docs.microsoft.com/en-us/azure/cosmos-db/linux-emulator?tabs=sql-api%2Cssl-netstd21#my-app-cant-connect-to-emulator-endpoint-the-tlsssl-connection-couldnt-be-established-or-i-cant-start-the-data-explorer
                HttpClientFactory = () =>
                {
                    HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    return new HttpClient(httpMessageHandler);
                },
                ConnectionMode = AzCosmos.ConnectionMode.Gateway,
                RequestTimeout = TimeSpan.FromMinutes(3)
            };

            var endpoint = Environment.GetEnvironmentVariable("CoreEx_Cosmos_Test_Endpoint") ?? "https://localhost:8081";
            var token = Environment.GetEnvironmentVariable("CoreEx_Cosmos_Test_Token") ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            CosmosClient = new AzCosmos.CosmosClient(endpoint, token, cco);

            CosmosDatabase = (await CosmosClient.CreateDatabaseIfNotExistsAsync("CoreEx.Cosmos.Test").ConfigureAwait(false)).Database;

            Mapper ??= new AutoMapperWrapper(new AutoMapper.Mapper(new AutoMapper.MapperConfiguration(c =>
            {
                c.AddProfile<AutoMapperProfile>();
                c.CreateMap<Person1, Person1>();
                c.CreateMap<Person2, Person2>();
                c.CreateMap<Person3, Person3>();
            })));

            var c1 = await CosmosDatabase.ReplaceOrCreateContainerAsync(new AzCosmos.ContainerProperties
            {
                Id = "Persons1",
                PartitionKeyPath = partitionKeyPath,
                UniqueKeyPolicy = new AzCosmos.UniqueKeyPolicy { UniqueKeys = { new AzCosmos.UniqueKey { Paths = { "/name" } } } }
            }, 400).ConfigureAwait(false);

            var c2 = await CosmosDatabase.ReplaceOrCreateContainerAsync(new AzCosmos.ContainerProperties
            {
                Id = "Persons2",
                PartitionKeyPath = partitionKeyPath,
                UniqueKeyPolicy = new AzCosmos.UniqueKeyPolicy { UniqueKeys = { new AzCosmos.UniqueKey { Paths = { "/name" } } } }
            }, 400).ConfigureAwait(false);

            var c3 = await CosmosDatabase.ReplaceOrCreateContainerAsync(new AzCosmos.ContainerProperties
            {
                Id = "Persons3",
                PartitionKeyPath = valuePartitionKeyPath,
                UniqueKeyPolicy = new AzCosmos.UniqueKeyPolicy { UniqueKeys = { new AzCosmos.UniqueKey { Paths = { "/type", "/value/name" } } } }
            }, 400).ConfigureAwait(false);

            var c4 = await CosmosDatabase.ReplaceOrCreateContainerAsync(new AzCosmos.ContainerProperties
            {
                Id = "RefData",
                PartitionKeyPath = "/_partitionKey",
                UniqueKeyPolicy = new AzCosmos.UniqueKeyPolicy { UniqueKeys = { new AzCosmos.UniqueKey { Paths = { "/type", "/value/code" } } } }
            }, 400);

            var db = new CosmosDb(auth: false);

            var jdr = JsonDataReader.ParseYaml<CosmosDb>("Data.yaml");
            await db.Persons1.ImportBatchAsync(jdr);
            await db.Persons2.ImportBatchAsync(jdr);
            await db.Persons3.ImportValueBatchAsync(jdr);
            await db.ImportValueBatchAsync("Persons3", new Person1[] { new Person1 { Id = 100.ToGuid().ToString() } }); // Add other random "type" to Person3.

            jdr = JsonDataReader.ParseYaml<CosmosDb>("RefData.yaml", new JsonDataReaderArgs(new Text.Json.ReferenceDataContentJsonSerializer()));
            await db.ImportValueBatchAsync("RefData", jdr, new Type[] { typeof(Gender) });
        }
    }
}