namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerTimeToLiveTests : CosmosTestBase
{
    private const string ContainerId = "ttl-items";

    [Test]
    public async Task CreateAsync_WithTimeToLive_ComputesAndPersistsTtl()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var container = CreateCosmosDb().Container<TestItem>(ContainerId, o => o
            .WithPartitionKey(m => m.PartitionKey)
            .WithTimeToLive(_ => 3600));

        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "Expiring" });

        created.Value.TimeToLive.Should().Be(3600);

        // Confirm it was actually persisted (not just present on the in-memory returned instance).
        var fetched = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched!.TimeToLive.Should().Be(3600);
    }

    [Test]
    public async Task UpdateAsync_WithTimeToLive_RecomputesTtl()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var ttlSeconds = 60;
        var container = CreateCosmosDb().Container<TestItem>(ContainerId, o => o
            .WithPartitionKey(m => m.PartitionKey)
            .WithTimeToLive(_ => ttlSeconds));

        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "Original" });

        ttlSeconds = 120;
        var toUpdate = created.Value;
        toUpdate.Name = "Updated";
        var updated = await container.UpdateAsync(toUpdate);

        updated.Value.TimeToLive.Should().Be(120);
    }

    [Test]
    public void WithTimeToLive_ModelWithoutITimeToLive_Throws()
    {
        var options = new CosmosDbModelOptions<NoTimeToLiveItem>();
        options.TimeToLiveSupport.IsMutable.Should().BeFalse();

        Assert.Throws<NotSupportedException>(() => options.WithTimeToLive(_ => 60));
    }
}
