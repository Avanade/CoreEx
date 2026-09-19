namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerFixedPartitionKeyTests : CosmosTestBase
{
    private const string ContainerId = "fixed-pk-items";

    [Test]
    public async Task WithFixedPartitionKey_CreateGetDelete_NoExplicitPartitionKeyNeeded()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var fixedKey = NewId();
        var container = CreateCosmosDb().Container<TestItem>(ContainerId, o => o.WithFixedPartitionKey(fixedKey));

        var id = NewId();

        // No PartitionKey set on the model itself - the fixed value resolves it (and is written back onto the model) on Create.
        var created = await container.CreateAsync(new TestItem { Id = id, Name = "Widget" });
        created.Value.Name.Should().Be("Widget");
        created.Value.PartitionKey.Should().Be(fixedKey);

        // Get/Delete omit the partitionKey parameter entirely - falls back to the configured fixed value.
        var fetched = await container.GetAsync(CompositeKey.Create(id));
        fetched!.Name.Should().Be("Widget");

        var deleted = await container.DeleteAsync(CompositeKey.Create(id));
        deleted.WasMutated.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_WithFixedPartitionKey_ModelAlreadyMatchesFromCreate_NoMismatchThrown()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var fixedKey = NewId();
        var container = CreateCosmosDb().Container<TestItem>(ContainerId, o => o.WithFixedPartitionKey(fixedKey));

        var id = NewId();
        var created = await container.CreateAsync(new TestItem { Id = id, Name = "Original" });

        // The model returned from Create already has PartitionKey written back to the fixed value (per the prior test) - updating
        // it should succeed without tripping the "model disagrees with configured value" mismatch check, since the two now agree.
        var toUpdate = created.Value;
        toUpdate.Name = "Updated";
        var updated = await container.UpdateAsync(toUpdate);

        updated.WasMutated.Should().BeTrue();
        updated.Value.Name.Should().Be("Updated");
        updated.Value.PartitionKey.Should().Be(fixedKey);

        // Confirm it was actually persisted (Get again omitting the partitionKey parameter, as in the Create/Get/Delete test).
        var fetched = await container.GetAsync(CompositeKey.Create(id));
        fetched!.Name.Should().Be("Updated");
        fetched.PartitionKey.Should().Be(fixedKey);
    }
}
