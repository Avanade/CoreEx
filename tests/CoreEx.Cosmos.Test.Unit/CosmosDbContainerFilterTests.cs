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

        var blocked = await container.GetWithResultAsync(CompositeKey.Create(id), id);
        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Should().BeOfType<AuthenticationException>();

        var bypassed = await container.GetWithResultAsync(new CosmosDbArgs { BypassFilters = true }, CompositeKey.Create(id), id);
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

        var blocked = await container.DeleteWithResultAsync(CompositeKey.Create(id), id);
        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Should().BeOfType<AuthenticationException>();

        var bypassed = await container.DeleteWithResultAsync(new CosmosDbArgs { BypassFilters = true }, CompositeKey.Create(id), id);
        bypassed.IsSuccess.Should().BeTrue();
        bypassed.Value.WasMutated.Should().BeTrue();
    }

    [Test]
    public async Task Update_WithNonQueryFilter_ReturnsConfiguredErrorUnlessBypassed()
    {
        // Same shape as Delete's equivalent test above - HasFilters forces Update's generalized pre-read, whose CheckModel now enforces a non-query filter's configured Result against the PERSISTED
        // document (not the incoming replace payload), rather than allowing a blind replace of a filtered-out (e.g. unauthorized) item.
        var container = await GetContainerAsync((_, _) => Result.AuthenticationError(), allowFilterBypass: true);
        var id = NewId();

        // Seed directly via the raw SDK container, bypassing CoreEx.Cosmos's own filter enforcement on Create.
        await container.Container.CreateItemAsync(new TestItem { Id = id, PartitionKey = id, Name = "Hidden" }, new PartitionKey(id));

        var blocked = await container.UpdateWithResultAsync(new TestItem { Id = id, PartitionKey = id, Name = "Overwritten" });
        blocked.IsFailure.Should().BeTrue();
        blocked.Error.Should().BeOfType<AuthenticationException>();

        var bypassed = await container.UpdateWithResultAsync(new CosmosDbArgs { BypassFilters = true }, new TestItem { Id = id, PartitionKey = id, Name = "Overwritten" });
        bypassed.IsSuccess.Should().BeTrue();
        bypassed.Value.Value.Name.Should().Be("Overwritten");
    }

    [Test]
    public async Task AsQueryable_WithBypassFilters_OnlyBypassesFiltersRegisteredAsBypassable()
    {
        // Query-only filter registered WITHOUT allowFilterBypass (defaults to false) - CosmosDbArgs.BypassFilters must have no effect on it; matching CoreEx.EntityFrameworkCore.EfDbModelOptions.ApplyFilters,
        // the call site (CosmosDbQuery.AsQueryable) always invokes ApplyFilters and the bypass decision is made per-registration, inside ApplyFilters, not by skipping it altogether.
        var container = await GetContainerAsync();
        var pk = NewId();

        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Visible" });
        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Hidden" });

        var query = container.Query(q => q.Where(m => m.PartitionKey == pk));

        var filteredItems = await DrainAsync(query.AsQueryable());
        filteredItems.Select(m => m.Name).Should().BeEquivalentTo(["Visible"]);

        // Not registered with allowFilterBypass: true, so BypassFilters must NOT surface "Hidden".
        var bypassedItems = await DrainAsync(query.AsQueryable(new CosmosDbArgs { BypassFilters = true }));
        bypassedItems.Select(m => m.Name).Should().BeEquivalentTo(["Visible"]);
    }

    [Test]
    public async Task AsQueryable_WithBypassFilters_BypassesFilterRegisteredAsBypassable()
    {
        // Query-only filter registered WITH allowFilterBypass: true - CosmosDbArgs.BypassFilters should surface the otherwise-excluded item, per the documented per-registration opt-in contract.
        var container = await GetContainerAsync(allowFilterBypass: true);
        var pk = NewId();

        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Visible" });
        await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = "Hidden" });

        var query = container.Query(q => q.Where(m => m.PartitionKey == pk));

        var bypassedItems = await DrainAsync(query.AsQueryable(new CosmosDbArgs { BypassFilters = true }));
        bypassedItems.Select(m => m.Name).Should().BeEquivalentTo(["Visible", "Hidden"]);
    }

    [Test]
    public async Task AsQueryable_WithBypassFilters_TenantFilterStillApplies()
    {
        // The mandatory tenant filter (WithTenantFilter, no allowFilterBypass parameter exists for it at all) must remain applied by AsQueryable regardless of CosmosDbArgs.BypassFilters - this is
        // the exact regression covered by the review comment: the prior implementation short-circuited ApplyFilters entirely on BypassFilters, silently exposing other tenants' documents to a query.
        const string containerId = "tenant-filter-items";
        await GetOrCreateContainerAsync(containerId).ConfigureAwait(false);

        var containerA = CreateCosmosDb("tenant-a").Container<TenantItem>(containerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTenantFilter());
        var containerB = CreateCosmosDb("tenant-b").Container<TenantItem>(containerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTenantFilter());

        await containerA.CreateAsync(new TenantItem { Id = NewId(), PartitionKey = NewId(), Name = "Owned by tenant-a" });

        var items = await DrainAsync(containerB.Query().AsQueryable(new CosmosDbArgs { BypassFilters = true }));
        items.Should().BeEmpty();
    }

    [Test]
    public async Task AsQueryable_WithBypassFilters_LogicalDeleteFilterStillApplies()
    {
        // The mandatory logical-delete filter (WithLogicalDeleteFilter, no allowFilterBypass parameter exists for it at all) must remain applied by AsQueryable regardless of CosmosDbArgs.BypassFilters.
        const string containerId = "soft-delete-filter-items";
        await GetOrCreateContainerAsync(containerId).ConfigureAwait(false);

        var container = CreateCosmosDb().Container<SoftDeleteItem>(containerId, o => o.WithPartitionKey(m => m.PartitionKey).WithLogicalDeleteFilter());
        var pk = NewId();

        var deletedId = NewId();
        await container.CreateAsync(new SoftDeleteItem { Id = NewId(), PartitionKey = pk, Name = "Visible" });
        await container.CreateAsync(new SoftDeleteItem { Id = deletedId, PartitionKey = pk, Name = "Deleted" });
        await container.DeleteAsync(CompositeKey.Create(deletedId), pk); // Logical delete - sets IsDeleted = true rather than physically removing the document.

        var items = await DrainAsync(container.Query(q => q.Where(m => m.PartitionKey == pk)).AsQueryable(new CosmosDbArgs { BypassFilters = true }));
        items.Select(m => m.Name).Should().BeEquivalentTo(["Visible"]);
    }

    [Test]
    public async Task UpdateAsync_ReturnsNotFound_WhenPersistedItemIsLogicallyDeleted()
    {
        // "No undelete via Update" - matching CoreEx.EntityFrameworkCore.EfDbModel's own CheckModel behavior (used as the consistency reference for this fix), LogicalDeleteSupport.IsSupported forces
        // Update's generalized pre-read, whose CheckModel rejects a replace targeting a persisted-but-logically-deleted document, rather than silently reviving it with the incoming payload's content.
        const string containerId = "soft-delete-update-items";
        await GetOrCreateContainerAsync(containerId).ConfigureAwait(false);

        var container = CreateCosmosDb().Container<SoftDeleteItem>(containerId, o => o.WithPartitionKey(m => m.PartitionKey).WithLogicalDeleteFilter());
        var pk = NewId();
        var id = NewId();

        await container.CreateAsync(new SoftDeleteItem { Id = id, PartitionKey = pk, Name = "Deleted" });
        await container.DeleteAsync(CompositeKey.Create(id), pk); // Logical delete - sets IsDeleted = true rather than physically removing the document.

        var result = await container.UpdateWithResultAsync(new SoftDeleteItem { Id = id, PartitionKey = pk, Name = "Resurrected" });
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    private static async Task<List<TestItem>> DrainAsync(IQueryable<TestItem> queryable)
    {
        var items = new List<TestItem>();
        using var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));

        return items;
    }

    private static async Task<List<TenantItem>> DrainAsync(IQueryable<TenantItem> queryable)
    {
        var items = new List<TenantItem>();
        using var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));

        return items;
    }

    private static async Task<List<SoftDeleteItem>> DrainAsync(IQueryable<SoftDeleteItem> queryable)
    {
        var items = new List<SoftDeleteItem>();
        using var iterator = queryable.ToFeedIterator();

        while (iterator.HasMoreResults)
            items.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));

        return items;
    }
}
