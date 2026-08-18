namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerFilterTests : CosmosTestBase
{
    private const string ContainerId = "filter-items";

    private static async Task<CosmosDbContainer<TestItem>> GetContainerAsync(Func<TestItem, OperationType, Result>? nonQueryResult = null, bool allowFilterBypass = false)
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        return CreateCosmosDb().Container<TestItem>(ContainerId, o =>
        {
            o.WithPartitionKey(m => m.PartitionKey);
            o.WithFilter(q => q.Where(m => !m.Name.StartsWith("Hidden")), nonQueryResult, allowFilterBypass);
        });
    }

    [Test]
    public async Task Query_WithFilter_ExcludesFilteredItems()
    {
        // Query-only filter (no nonQueryResult) - Create is unaffected, only Query excludes matches.
        var container = await GetContainerAsync();
        var pk = NewId();

        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Visible" });
        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Hidden" });

        var items = await container.Query(q => q.Where(m => m.PartitionKey == pk)).ToListAsync();

        items.Should().ContainSingle();
        items[0].Name.Should().Be("Visible");
    }

    [Test]
    public async Task Get_WithNonQueryFilter_ReturnsConfiguredErrorUnlessBypassed()
    {
        // Non-query filter with a nonQueryResult and allowFilterBypass - Get on a filtered-out item fails with the
        // configured Result unless CosmosDbArgs.BypassFilters is set.
        var container = await GetContainerAsync((_, _) => Result.AuthenticationError(), allowFilterBypass: true);
        var id = NewId();

        // Seed directly via the raw SDK container, bypassing CoreEx.Cosmos's own filter enforcement on Create.
        await container.Container.CreateItemAsync(new TestItem { Id = id, PartitionKey = id, Name = "Hidden" }, new PartitionKey(id));

        var blocked = await container.GetWithResultAsync(CompositeKey.Create(id), new PartitionKey(id));
        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Should().BeOfType<AuthenticationException>();

        var bypassed = await container.GetWithResultAsync(new CosmosDbArgs { BypassFilters = true }, CompositeKey.Create(id), new PartitionKey(id));
        bypassed.IsSuccess.Should().BeTrue();
        bypassed.Value.Name.Should().Be("Hidden");
    }

    [Test]
    public async Task Delete_WithNonQueryFilter_ReturnsConfiguredErrorUnlessBypassed()
    {
        // Non-query filter with a nonQueryResult and allowFilterBypass - Delete on a filtered-out item fails with the configured Result unless CosmosDbArgs.BypassFilters is set (the presence of the
        // filter forces Delete's pre-read path, since there is now something for CheckModel to check).
        var container = await GetContainerAsync((_, _) => Result.AuthenticationError(), allowFilterBypass: true);
        var id = NewId();

        // Seed directly via the raw SDK container, bypassing CoreEx.Cosmos's own filter enforcement on Create.
        await container.Container.CreateItemAsync(new TestItem { Id = id, PartitionKey = id, Name = "Hidden" }, new PartitionKey(id));

        var blocked = await container.DeleteWithResultAsync(CompositeKey.Create(id), new PartitionKey(id));
        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Should().BeOfType<AuthenticationException>();

        var bypassed = await container.DeleteWithResultAsync(new CosmosDbArgs { BypassFilters = true }, CompositeKey.Create(id), new PartitionKey(id));
        bypassed.IsSuccess.Should().BeTrue();
        bypassed.Value.WasMutated.Should().BeTrue();
    }

    [Test]
    public async Task AsQueryable_WithBypassFilters_SurfacesFilteredItems()
    {
        // Query-only filter (no nonQueryResult, no allowFilterBypass) - AsQueryable's own CosmosDbArgs.BypassFilters is a broader mechanism than the per-filter allowFilterBypass opt-in: it skips
        // ApplyFilters entirely, so it surfaces items that would otherwise be excluded even when the underlying WithFilter was not itself registered as bypassable.
        var container = await GetContainerAsync();
        var pk = NewId();

        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Visible" });
        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Hidden" });

        var query = container.Query(q => q.Where(m => m.PartitionKey == pk));

        var filteredItems = await DrainAsync(query.AsQueryable());
        filteredItems.Select(m => m.Name).Should().BeEquivalentTo(["Visible"]);

        var bypassedItems = await DrainAsync(query.AsQueryable(new CosmosDbArgs { BypassFilters = true }));
        bypassedItems.Select(m => m.Name).Should().BeEquivalentTo(["Visible", "Hidden"]);
    }

    private static async Task<List<TestItem>> DrainAsync(IQueryable<TestItem> queryable)
    {
        var items = new List<TestItem>();
        using var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));

        return items;
    }
}
