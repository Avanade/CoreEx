namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Proves the <c>CoreEx.UnitTesting.Cosmos</c> container-reset + YAML-driven batch-import primitives end-to-end: reset a container to a known-empty state, seed it from an embedded
/// <c>*.seed.yaml</c> fixture via <see cref="JsonDataReader"/> + <c>ImportBatchAsync</c>, then read/assert against the seeded data - directly analogous to the SQL Server/Postgres samples'
/// <c>Test.MigratePostgresDataAsync</c>/<c>MigrateSqlServerDataAsync</c> seeding pattern, for containers rather than relational tables.
/// </summary>
[TestFixture]
public class CosmosDbSeedingTests : CosmosTestBase
{
    private const string ContainerId = "seed-read-items";
    private static Container? _seededContainer;

    /// <summary>
    /// Resets (deletes + recreates) <see cref="ContainerId"/> and seeds it from <c>Data/read-data.seed.yaml</c>, once per test run.
    /// </summary>
    private static async Task<Container> GetSeededContainerAsync()
    {
        if (_seededContainer is not null)
            return _seededContainer;

        var container = await TestDatabase.ReplaceOrCreateContainerAsync(ContainerId, "/partitionKey").ConfigureAwait(false);

        var jdr = JsonDataReader.ParseYaml<CosmosDbSeedingTests>("read-data.seed.yaml");
        (await container.ImportBatchAsync(jdr, ContainerId).ConfigureAwait(false)).Should().BeTrue("the fixture's top-level 'seed-read-items' key must resolve to an array of items to import");

        return _seededContainer = container;
    }

    [Test]
    public async Task ImportBatchAsync_SeedsContainer_ThenQueryableViaCosmosDbContainer()
    {
        await GetSeededContainerAsync();

        var items = await CreateCosmosDb().Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey))
            .Query(q => q.Where(m => m.PartitionKey == "seed-pk").OrderBy(m => m.Name))
            .ToListAsync();

        items.Should().HaveCount(3);
        items.Select(m => m.Name).Should().ContainInOrder("Item-01", "Item-02", "Item-03");
    }
}
