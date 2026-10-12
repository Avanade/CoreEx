namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbChangeTrackerTests : CosmosTestBase
{
    private const string ContainerId = "tracker-items";

    private static async Task<(CosmosDb Db, CosmosDbContainer<TestItem> Container)> GetAsync()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var db = CreateCosmosDb();
        return (db, db.Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey)));
    }

    private static async Task<(string Id, TestItem Created)> SeedAsync(string name = "Original")
    {
        var (_, c) = await GetAsync();
        var id = NewId();
        var created = (await c.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = name })).Value;
        return (id, created);
    }

    [Test]
    public async Task Get_SecondRead_IsServedFromSnapshot()
    {
        var (id, _) = await SeedAsync();
        var (db, scoped) = await GetAsync();

        var first = await scoped.GetAsync(CompositeKey.Create(id), id);
        db.ChangeTracker.Count.Should().Be(1);

        // Change the document from a different scope; this scope must still see the snapshot.
        var (_, other) = await GetAsync();
        var fresh = (await other.GetAsync(CompositeKey.Create(id), id))!;
        fresh.Name = "Changed";
        await other.UpdateAsync(fresh);

        var second = await scoped.GetAsync(CompositeKey.Create(id), id);
        second!.Name.Should().Be("Original");
        second.Should().NotBeSameAs(first);
    }

    [Test]
    public async Task Get_MutatingReturnedInstance_DoesNotCorruptSnapshot()
    {
        var (id, _) = await SeedAsync();
        var (_, c) = await GetAsync();

        var first = (await c.GetAsync(CompositeKey.Create(id), id))!;
        first.Name = "Tampered";

        var second = await c.GetAsync(CompositeKey.Create(id), id);
        second!.Name.Should().Be("Original");
    }

    [Test]
    public async Task Get_WithClearChangeTrackerAfterGet_DoesNotTrack_AndBypassesSnapshot()
    {
        var (id, _) = await SeedAsync();
        var (db, c) = await GetAsync();

        await c.GetAsync(c.Args with { ClearChangeTrackerAfterGet = true }, CompositeKey.Create(id), id);
        db.ChangeTracker.Count.Should().Be(0);

        await c.GetAsync(CompositeKey.Create(id), id);
        db.ChangeTracker.Count.Should().Be(1);

        var (_, other) = await GetAsync();
        var fresh = (await other.GetAsync(CompositeKey.Create(id), id))!;
        fresh.Name = "Changed";
        await other.UpdateAsync(fresh);

        var bypass = await c.GetAsync(c.Args with { ClearChangeTrackerAfterGet = true }, CompositeKey.Create(id), id);
        bypass!.Name.Should().Be("Changed");
        db.ChangeTracker.Count.Should().Be(0);
    }

    [Test]
    public async Task Get_NotFound_IsNotTracked()
    {
        var (db, c) = await GetAsync();
        var id = NewId();
        (await c.GetAsync(CompositeKey.Create(id), id)).Should().BeNull();
        db.ChangeTracker.Count.Should().Be(0);
    }

    [Test]
    public async Task Update_Evicts_AndSubsequentGetSeesNewValue()
    {
        var (id, _) = await SeedAsync();
        var (db, c) = await GetAsync();

        var item = (await c.GetAsync(CompositeKey.Create(id), id))!;
        item.Name = "Updated";
        await c.UpdateAsync(item);

        db.ChangeTracker.Count.Should().Be(0);
        (await c.GetAsync(CompositeKey.Create(id), id))!.Name.Should().Be("Updated");
    }

    [Test]
    public async Task Delete_BypassesSnapshot_AndEvicts()
    {
        var (id, _) = await SeedAsync();
        var (db, c) = await GetAsync();

        await c.GetAsync(CompositeKey.Create(id), id);
        db.ChangeTracker.Count.Should().Be(1);

        // Delete from elsewhere; the snapshot here is now stale.
        var (_, other) = await GetAsync();
        await other.DeleteAsync(CompositeKey.Create(id), id);

        await c.DeleteAsync(CompositeKey.Create(id), id);
        db.ChangeTracker.Count.Should().Be(0);
        (await c.GetAsync(CompositeKey.Create(id), id)).Should().BeNull();
    }

    [Test]
    public async Task Update_WithoutChangeLogOnCandidate_RetainsCreatedAudit()
    {
        var (id, created) = await SeedAsync();
        var (_, c) = await GetAsync();

        created.ChangeLog.Should().NotBeNull();

        var candidate = new TestItem { Id = id, PartitionKey = id, Name = "Replaced" };
        var updated = (await c.UpdateAsync(candidate)).Value;

        updated.ChangeLog!.CreatedOn.Should().Be(created.ChangeLog!.CreatedOn);
        updated.ChangeLog.CreatedBy.Should().Be(created.ChangeLog.CreatedBy);
        updated.ChangeLog.UpdatedOn.Should().NotBeNull();
    }

    [Test]
    public async Task Update_ForwardsExtensionData_CandidateKeysWin()
    {
        var (id, _) = await SeedAsync();
        var (db, c) = await GetAsync();

        // Write unmapped properties directly via the SDK, as another (newer) version of the model might have.
        var raw = Client.GetContainer(TestDatabase.Id, ContainerId);
        var doc = (await raw.ReadItemAsync<System.Text.Json.Nodes.JsonObject>(id, new Microsoft.Azure.Cosmos.PartitionKey(id))).Resource;
        doc["keep"] = "kept";
        doc["both"] = "server";
        await raw.ReplaceItemAsync(doc, id, new Microsoft.Azure.Cosmos.PartitionKey(id));

        var candidate = new TestItem { Id = id, PartitionKey = id, Name = "Replaced", ExtensionData = new() { ["both"] = "candidate" } };
        await c.UpdateAsync(candidate);

        var after = (await raw.ReadItemAsync<System.Text.Json.Nodes.JsonObject>(id, new Microsoft.Azure.Cosmos.PartitionKey(id))).Resource;
        after["keep"]!.GetValue<string>().Should().Be("kept");
        after["both"]!.GetValue<string>().Should().Be("candidate");
        after["name"]!.GetValue<string>().Should().Be("Replaced");
        db.ChangeTracker.Count.Should().Be(0);
    }

    [Test]
    public async Task Update_NoChanges_IsNoOp_AndDoesNotMutate()
    {
        var (id, _) = await SeedAsync();
        var (_, c) = await GetAsync();

        var item = (await c.GetAsync(CompositeKey.Create(id), id))!;
        var etag = item.ETag;

        var result = await c.UpdateAsync(item);
        result.WasMutated.Should().BeFalse();
        result.Value.ETag.Should().Be(etag);

        var raw = Client.GetContainer(TestDatabase.Id, ContainerId);
        (await raw.ReadItemAsync<System.Text.Json.Nodes.JsonObject>(id, new Microsoft.Azure.Cosmos.PartitionKey(id))).Resource["_etag"]!.GetValue<string>().Should().Be(etag);
    }

    [Test]
    public async Task Update_NoChanges_WithStaleETag_StillHitsServer_AndThrowsConcurrency()
    {
        var (id, created) = await SeedAsync();

        // Move the server on so the created ETag is stale.
        var (_, other) = await GetAsync();
        var fresh = (await other.GetAsync(CompositeKey.Create(id), id))!;
        fresh.Name = "Winner";
        await other.UpdateAsync(fresh);

        // The candidate is value-identical to the persisted document but carries the stale ETag, so the no-op shortcut must not apply.
        var (_, c) = await GetAsync();
        var stale = new TestItem { Id = id, PartitionKey = id, Name = "Winner", ETag = created.ETag };
        var act = () => c.UpdateAsync(stale);
        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Test]
    public async Task Update_Changed_IsMutated()
    {
        var (id, _) = await SeedAsync();
        var (_, c) = await GetAsync();

        var item = (await c.GetAsync(CompositeKey.Create(id), id))!;
        item.Name = "Different";
        (await c.UpdateAsync(item)).WasMutated.Should().BeTrue();
    }

    [Test]
    public async Task UnitOfWork_RootTransaction_ClearsTrackerOnCompletion()
    {
        var (id, _) = await SeedAsync();
        var (db, c) = await GetAsync();
        var uow = new CosmosDbUnitOfWork(db);

        await uow.TransactionAsync(async ct =>
        {
            var item = (await c.GetAsync(CompositeKey.Create(id), id, ct).ConfigureAwait(false))!;
            db.ChangeTracker.Count.Should().Be(1);
            item.Name = "InTxn";
            await c.UpdateAsync(item, ct).ConfigureAwait(false);
        });

        db.ChangeTracker.Count.Should().Be(0);
        (await c.GetAsync(CompositeKey.Create(id), id))!.Name.Should().Be("InTxn");
    }
}