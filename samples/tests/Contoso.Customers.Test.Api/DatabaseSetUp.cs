namespace Contoso.Customers.Test.Api;

internal static class DatabaseSetUp
{
    /// <param name="tester">The <see cref="TesterBase"/> - the <see cref="Database"/> is resolved from its running host's own <see cref="ICosmosDb"/> registration (see
    /// <see cref="UnitTestExExtensions.GetCosmosDatabaseAsync"/>), guaranteeing this always seeds the exact database/containers the API host itself reads from.</param>
    /// <param name="customerSeedFileNames">Zero or more embedded YAML resource names (see <see cref="Contoso.Customers.Test.Common.TestData"/>), each with a top-level 'customers' key, used to seed the
    /// (freshly reset) 'customers' container - raw camelCase document shape (matching <c>Contoso.Customers.Infrastructure.Persistence.Customer</c>'s JSON property names), imported via
    /// <see cref="CosmosDbBatch.ImportBatchAsync(Database, JsonDataReader, bool, CancellationToken)"/>.</param>
    public static async Task DatabaseSetUpAsync(this TesterBase tester, params string[] customerSeedFileNames)
    {
        var database = await tester.GetCosmosDatabaseAsync().ConfigureAwait(false);

        // Replace or create "customers" container used by the API. The partition key is assumed to be "/partitionKey" for all containers, which is a common pattern for Cosmos DB.
        await database.ReplaceOrCreateContainerAsync("customers", "/partitionKey").ConfigureAwait(false);

        // Seed any known precondition rows (e.g. mutate-data.seed.yaml) directly into the freshly reset container - no TModel typing involved, raw JSON straight through.
        foreach (var fileName in customerSeedFileNames)
        {
            var customerJdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>(fileName, new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase));
            await CosmosDbBatch.ImportBatchAsync(database, customerJdr).ConfigureAwait(false);
        }

        // Replace or create "ref-data" container used by the API. Reuse the "test" configured reference data and import.
        await database.ReplaceOrCreateContainerAsync("ref-data", "/partitionKey").ConfigureAwait(false);
        var jdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        await CosmosDbBatch.ImportDiscriminatedBatchAsync(database, jdr).ConfigureAwait(false);
    }
}
