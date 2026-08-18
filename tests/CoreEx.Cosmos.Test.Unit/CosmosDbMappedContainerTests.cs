namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbMappedContainerTests : CosmosTestBase
{
    private const string ContainerId = "mapped-items";

    private static async Task<CosmosDbMappedContainer<TestValue, TestItem, TestValueMapper>> GetMappedContainerAsync()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var container = CreateCosmosDb().Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        return container.ToMappedModel<TestValue, TestValueMapper>(new TestValueMapper());
    }

    [Test]
    public async Task CreateAsync_GetAsync_UpdateAsync_DeleteAsync_RoundTripsAsContractValue()
    {
        var mapped = await GetMappedContainerAsync();
        var id = NewId();

        var created = await mapped.CreateAsync(new TestValue { Id = id, Name = "Contract" });
        created.Value.Should().BeOfType<TestValue>();
        created.Value.Id.Should().Be(id);
        created.Value.Name.Should().Be("Contract");
        created.Value.ETag.Should().NotBeNullOrEmpty();

        var fetched = await mapped.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Contract");

        fetched.Name = "Updated Contract";
        var updated = await mapped.UpdateAsync(fetched);
        updated.Value.Name.Should().Be("Updated Contract");

        var deleted = await mapped.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id));
        deleted.WasMutated.Should().BeTrue();

        var afterDelete = await mapped.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        afterDelete.Should().BeNull();
    }
}
