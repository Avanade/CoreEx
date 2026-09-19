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
        var deleted = await containerB.DeleteAsync(CompositeKey.Create(id), id);
        deleted.WasMutated.Should().BeFalse();

        // Confirm it still exists, untouched, for Tenant A.
        var stillThere = await containerA.GetAsync(CompositeKey.Create(id), id);
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

        var deleted = await containerA.DeleteAsync(CompositeKey.Create(id), id);
        deleted.WasMutated.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_CrossTenant_ReturnsNotFound_DoesNotUpdate()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var containerA = CreateCosmosDb("tenant-a").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
        var containerB = CreateCosmosDb("tenant-b").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var id = NewId();
        await containerA.CreateAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Owned by tenant-a" });

        // Tenant B attempts to blindly replace Tenant A's document by (known/guessed) id + partition key - TenantSupport.IsSupported forces Update's pre-read, whose CheckModel rejects the cross-tenant
        // read, so this is treated as not-found rather than silently overwriting Tenant A's document with Tenant B's content.
        var result = await containerB.UpdateWithResultAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Hijacked by tenant-b" });
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();

        // Confirm it still exists, untouched, for Tenant A.
        var stillThere = await containerA.GetAsync(CompositeKey.Create(id), id);
        stillThere.Should().NotBeNull();
        stillThere!.Name.Should().Be("Owned by tenant-a");
    }

    [Test]
    public async Task UpdateAsync_SameTenant_Succeeds()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        var containerA = CreateCosmosDb("tenant-a").Container<TenantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));

        var id = NewId();
        await containerA.CreateAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Owned by tenant-a" });

        var updated = await containerA.UpdateAsync(new TenantItem { Id = id, PartitionKey = id, Name = "Renamed by tenant-a" });
        updated.Value.Name.Should().Be("Renamed by tenant-a");
    }
}
