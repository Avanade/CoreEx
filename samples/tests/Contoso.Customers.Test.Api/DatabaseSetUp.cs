namespace Contoso.Customers.Test.Api;

internal static class DatabaseSetUp
{
    /// <param name="tester">The <see cref="TesterBase"/> - the <see cref="Database"/> is resolved from its running host's own <see cref="ICosmosDb"/> registration (see
    /// <see cref="UnitTestExExtensions.GetCosmosDatabaseAsync"/>), guaranteeing this always seeds the exact database/containers the API host itself reads from.</param>
    /// <param name="resourceFileNames">Zero or more embedded YAML resource names (see <see cref="Contoso.Customers.Test.Common.TestData"/>) imported via
    /// <see cref="CosmosDbBatch.ImportBatchAsync(Database, JsonDataReader, bool, CancellationToken)"/>.</param>
    public static async Task DatabaseSetUpAsync(this TesterBase tester, params string[] resourceFileNames)
    {
        var database = await tester.GetCosmosDatabaseAsync().ConfigureAwait(false);

        // Replace or create "customers" container used by the API. The partition key is assumed to be "/partitionKey" for all containers, which is a common pattern for Cosmos DB.
        // CustomerQueryArgsConfig's "LastName" order-by field is configured WithAlwaysInclude() (always appended, in its own default ascending direction, regardless of what the caller actually
        // requested) - so ordering by "FirstName" always produces a two-property ORDER BY (e.g. "firstName DESC, lastName ASC"), which Cosmos DB rejects outright unless a matching composite index
        // exists. Both direction combinations actually reachable via the API ($orderby=firstname[,desc]) are indexed here so CustomerReadTests' query tests can exercise both.
        var customersContainerProperties = new ContainerProperties("customers", "/partitionKey");
        customersContainerProperties.IndexingPolicy.CompositeIndexes.Add(
        [
            new() { Path = "/firstName", Order = CompositePathSortOrder.Ascending },
            new() { Path = "/lastName", Order = CompositePathSortOrder.Ascending }
        ]);
        customersContainerProperties.IndexingPolicy.CompositeIndexes.Add(
        [
            new() { Path = "/firstName", Order = CompositePathSortOrder.Descending },
            new() { Path = "/lastName", Order = CompositePathSortOrder.Ascending }
        ]);

        await database.ReplaceOrCreateContainerAsync(customersContainerProperties).ConfigureAwait(false);

        // Seed any known precondition rows (e.g. mutate-data.seed.yaml) directly into the freshly reset container - no TModel typing involved, raw JSON straight through.
        foreach (var fileName in resourceFileNames)
        {
            var customerJdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>(fileName, new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase));
            await CosmosDbBatch.ImportBatchAsync(database, customerJdr).ConfigureAwait(false);
        }

        // Replace or create "ref-data" container used by the API. Reuse the "test" configured reference data and import. A unique key policy on "/typeDiscriminator" and "/code" enforces
        // (per logical partition) that no two documents share the same discriminator/code combination - the closest Cosmos DB equivalent to a unique index.
        var refDataContainerProperties = new ContainerProperties("ref-data", "/partitionKey");
        refDataContainerProperties.UniqueKeyPolicy.UniqueKeys.Add(new UniqueKey { Paths = { "/typeDiscriminator", "/code" } });

        await database.ReplaceOrCreateContainerAsync(refDataContainerProperties).ConfigureAwait(false);

        var jdr = JsonDataReader.ParseYaml<Contoso.Customers.Test.Common.TestData>("ref-data.seed.yaml", JsonDataReaderOptions.CreateForReferenceData(JsonPropertyNamingConvention.CamelCase));
        await CosmosDbBatch.ImportDiscriminatedBatchAsync(database, jdr).ConfigureAwait(false);
    }
}
