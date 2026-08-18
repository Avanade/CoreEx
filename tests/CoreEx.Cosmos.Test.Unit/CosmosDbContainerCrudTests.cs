namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerCrudTests : CosmosTestBase
{
    private static async Task<CosmosDbContainer<TestItem>> GetContainerAsync()
    {
        await GetOrCreateContainerAsync("crud-items").ConfigureAwait(false);
        return CreateCosmosDb().Container<TestItem>("crud-items", o => o.WithPartitionKey(m => m.PartitionKey));
    }

    [Test]
    public async Task CreateAsync_Then_GetAsync_RoundTrips()
    {
        var container = await GetContainerAsync();
        var id = NewId();
        var model = new TestItem { Id = id, PartitionKey = id, Name = "Widget" };

        var created = await container.CreateAsync(model);
        created.WasMutated.Should().BeTrue();
        created.Value.Id.Should().Be(id);
        created.Value.Name.Should().Be("Widget");
        created.Value.ETag.Should().NotBeNullOrEmpty();

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Widget");
        fetched.ETag.Should().Be(created.Value.ETag);
    }

    [Test]
    public async Task UpdateAsync_PersistsChanges_AndChangesETag()
    {
        var container = await GetContainerAsync();
        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "Original" });

        var toUpdate = created.Value;
        toUpdate.Name = "Updated";
        var updated = await container.UpdateAsync(toUpdate);

        updated.WasMutated.Should().BeTrue();
        updated.Value.Name.Should().Be("Updated");
        updated.Value.ETag.Should().NotBe(created.Value.ETag);

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched!.Name.Should().Be("Updated");
    }

    [Test]
    public async Task DeleteAsync_RemovesItem_AndIsIdempotent()
    {
        var container = await GetContainerAsync();
        var id = NewId();
        await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "ToDelete" });

        var deleted = await container.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id));
        deleted.WasMutated.Should().BeTrue();

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched.Should().BeNull();

        // Idempotent: deleting again is not an error and reports no mutation.
        var deletedAgain = await container.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id));
        deletedAgain.WasMutated.Should().BeFalse();
    }

    [Test]
    public async Task UpsertAsync_CreatesWhenMissing_ThenUpdatesWhenExisting()
    {
        var container = await GetContainerAsync();
        var id = NewId();

        var upserted1 = await container.UpsertAsync(new TestItem { Id = id, PartitionKey = id, Name = "First" });
        upserted1.Value.Name.Should().Be("First");

        var toUpsert = upserted1.Value;
        toUpsert.Name = "Second";
        var upserted2 = await container.UpsertAsync(toUpsert);
        upserted2.Value.Name.Should().Be("Second");

        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched!.Name.Should().Be("Second");
    }
}
