namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerConcurrencyTests : CosmosTestBase
{
    private static async Task<CosmosDbContainer<TestItem>> GetContainerAsync()
    {
        await GetOrCreateContainerAsync("concurrency-items").ConfigureAwait(false);
        return CreateCosmosDb().Container<TestItem>("concurrency-items", o => o.WithPartitionKey(m => m.PartitionKey));
    }

    [Test]
    public async Task UpdateAsync_WithStaleETag_Throws_ConcurrencyException()
    {
        var container = await GetContainerAsync();
        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "Original" });

        // Simulate a concurrent update from elsewhere.
        var winner = created.Value;
        winner.Name = "Winner";
        await container.UpdateAsync(winner);

        // Attempt to update using the now-stale ETag captured before the winning update.
        var stale = created.Value;
        stale.Name = "Loser";

        Assert.ThrowsAsync<ConcurrencyException>(async () => await container.UpdateAsync(stale));
    }

    [Test]
    public async Task UpdateWithResultAsync_WithStaleETag_ReturnsConcurrencyError()
    {
        var container = await GetContainerAsync();
        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, PartitionKey = id, Name = "Original" });

        var winner = created.Value;
        winner.Name = "Winner";
        await container.UpdateAsync(winner);

        var stale = created.Value;
        stale.Name = "Loser";

        var result = await container.UpdateWithResultAsync(stale);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
    }
}
