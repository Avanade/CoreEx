namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerTenantTests : CosmosTestBase
{
    private const string ContainerId = "tenant-items";

    [Test]
    public async Task DeleteAsync_CrossTenant_TreatsAsNotFound_DoesNotDelete()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var containerA = CreateCosmosDb("tenant-a").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var containerB = CreateCosmosDb("tenant-b").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var id = NewId();
        await containerA.CreateAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Owned by tenant-a" });

        // Tenant B attempts to delete Tenant A's document by (known/guessed) id + partition key - the pre-read's tenant check (TenantSupport.IsSupported forces the pre-read path even with no
        // logical delete or WithFilter configured) means this is treated as not-found rather than actually deleting Tenant A's document.
        var deleted = await containerB.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id));
        deleted.WasMutated.Should().BeFalse();

        // Confirm it still exists, untouched, for Tenant A.
        var stillThere = await containerA.GetAsync(CompositeKey.Create(id), new PartitionKey(id));
        stillThere.Should().NotBeNull();
        stillThere!.Name.Should().Be("Owned by tenant-a");
    }

    [Test]
    public async Task DeleteAsync_SameTenant_Succeeds()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var containerA = CreateCosmosDb("tenant-a").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var id = NewId();
        await containerA.CreateAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Owned by tenant-a" });

        var deleted = await containerA.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id));
        deleted.WasMutated.Should().BeTrue();
    }
}
