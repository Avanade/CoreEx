namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerNotFoundTests : CosmosTestBase
{
    private static async Task<CosmosDbContainer<TestItem>> GetContainerAsync()
    {
        await GetOrCreateContainerAsync("notfound-items").ConfigureAwait(false);
        return CreateCosmosDb().Container<TestItem>("notfound-items", o => o.WithPartitionKey(m => m.PartitionKey));
    }

    [Test]
    public async Task GetAsync_ThrowingForm_NullOnNotFound_ReturnsNull()
    {
        var container = await GetContainerAsync();
        var id = NewId();

        var result = await container.GetAsync(CompositeKey.Create(id), new PartitionKey(id));

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ThrowingForm_NullOnNotFoundFalse_ThrowsNotFoundException()
    {
        var container = await GetContainerAsync();
        var id = NewId();

        Assert.ThrowsAsync<NotFoundException>(async () => await container.GetAsync(container.Args with { NullOnNotFound = false }, CompositeKey.Create(id), new PartitionKey(id)));
    }

    [Test]
    public async Task GetWithResultAsync_ReturnsNotFoundError()
    {
        var container = await GetContainerAsync();
        var id = NewId();

        var result = await container.GetWithResultAsync(CompositeKey.Create(id), new PartitionKey(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task DeleteAsync_NotFound_IsNotAnError()
    {
        var container = await GetContainerAsync();
        var id = NewId();

        var result = await container.DeleteWithResultAsync(CompositeKey.Create(id), new PartitionKey(id));

        result.IsSuccess.Should().BeTrue();
        result.Value.WasMutated.Should().BeFalse();
    }
}
