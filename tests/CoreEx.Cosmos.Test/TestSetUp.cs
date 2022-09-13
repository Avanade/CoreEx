using CoreEx.Mapping;
using CoreEx.Cosmos.Batch;
using AzCosmos = Microsoft.Azure.Cosmos;
using CoreEx.Json.Data;

namespace CoreEx.Cosmos.Test
{
    public static class TestSetUp
    {
        public static AzCosmos.CosmosClient? CosmosClient { get; private set; }

        public static AzCosmos.Database? CosmosDatabase { get; private set; }

        public static IMapper? Mapper { get; private set; }

        public static async Task SetUpAsync(string partitionKeyPath = "/_partitionKey", string valuePartitionKeyPath = "/_partitionKey")
        {
            var cco = new AzCosmos.CosmosClientOptions { SerializerOptions = new AzCosmos.CosmosSerializationOptions { PropertyNamingPolicy = AzCosmos.CosmosPropertyNamingPolicy.CamelCase, IgnoreNullValues = true } };
            CosmosClient = new AzCosmos.CosmosClient("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", cco);
            CosmosDatabase = (await CosmosClient.CreateDatabaseIfNotExistsAsync("CoreEx.Cosmos.Test").ConfigureAwait(false)).Database;

            Mapper = new AutoMapperWrapper(new AutoMapper.Mapper(new AutoMapper.MapperConfiguration(c =>
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