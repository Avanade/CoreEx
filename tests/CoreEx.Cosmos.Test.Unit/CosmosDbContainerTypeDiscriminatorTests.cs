namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies that two distinct business model types (<see cref="AnimalItem"/> and <see cref="PlantItem"/>) can safely share the same container/partition using
/// <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/> - no envelope/wrapper type required.
/// </summary>
[TestFixture]
public class CosmosDbContainerTypeDiscriminatorTests : CosmosTestBase
{
    private const string ContainerId = "discriminator-items";

    [Test]
    public async Task Query_OnlyReturnsMatchingTypeDiscriminator_WhenTypesShareContainerAndPartition()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        // One SHARED CosmosDb instance for both types - matching real usage (one scoped ICosmosDb injected into application code that then asks for Container<TModel>() against the same containerId for more
        // than one type). Using two separate CosmosDb instances here (as an earlier version of this test did) masks a real bug: CosmosDb/CosmosDbOptions used to cache per-containerId alone, so the second
        // type sharing a containerId from the SAME instance would throw InvalidCastException trying to cast the first type's cached CosmosDbContainer<TModel>/CosmosDbModelOptions<TModel> to its own.
        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var sharedPartition = NewId();

        // The type discriminator is auto-stamped by Model.PrepareCreate (via Model.PrepareTypeDiscriminator, from each model's [Schema(Name = ...)] attribute).
        var dog = await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Dog" });
        var cat = await animals.CreateAsync(new AnimalItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Cat" });
        var fern = await plants.CreateAsync(new PlantItem { Id = NewId(), PartitionKey = sharedPartition, Name = "Fern" });

        dog.Value.TypeDiscriminator.Should().Be(nameof(AnimalItem));
        fern.Value.TypeDiscriminator.Should().Be(nameof(PlantItem));

        var animalResults = await animals.Query(q => q.Where(m => m.PartitionKey == sharedPartition)).ToListAsync();
        animalResults.Select(m => m.Name).Should().BeEquivalentTo(["Dog", "Cat"]);

        var plantResults = await plants.Query(q => q.Where(m => m.PartitionKey == sharedPartition)).ToListAsync();
        plantResults.Select(m => m.Name).Should().BeEquivalentTo(["Fern"]);
    }

    [Test]
    public async Task GetAsync_ReturnsNotFound_WhenIdAndPartitionMatchADifferentConfiguredTypeDiscriminator()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var partitionKey = NewId();
        var id = NewId();
        await animals.CreateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Dog" });

        // Same id + partition, but requested as a PlantItem - CheckModel must reject the cross-type read rather than deserializing/returning the AnimalItem document.
        var result = await plants.GetWithResultAsync(CompositeKey.Create(id), partitionKey);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task UpdateAsync_ReturnsNotFound_WhenIdAndPartitionMatchADifferentConfiguredTypeDiscriminator()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var partitionKey = NewId();
        var id = NewId();
        await animals.CreateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Dog" });

        // A PlantItem replace targeting the AnimalItem's id/partition must not silently overwrite it with a differently-typed document.
        var result = await plants.UpdateWithResultAsync(new PlantItem { Id = id, PartitionKey = partitionKey, Name = "Fern" });
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();

        // Confirm the original AnimalItem document is untouched.
        var animal = await animals.GetAsync(CompositeKey.Create(id), partitionKey);
        animal.Should().NotBeNull();
        animal!.Name.Should().Be("Dog");
    }

    [Test]
    public async Task DeleteAsync_DoesNotDelete_WhenIdAndPartitionMatchADifferentConfiguredTypeDiscriminator()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator());

        var partitionKey = NewId();
        var id = NewId();
        await animals.CreateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Dog" });

        // A PlantItem delete targeting the AnimalItem's id/partition must not physically delete it - WithTypeDiscriminator forces the pre-read (fast-path is disabled) so CheckModel can reject it.
        var deleted = await plants.DeleteWithResultAsync(CompositeKey.Create(id), partitionKey);
        deleted.Value.WasMutated.Should().BeFalse();

        // Confirm the AnimalItem document still exists, untouched.
        var animal = await animals.GetAsync(CompositeKey.Create(id), partitionKey);
        animal.Should().NotBeNull();
        animal!.Name.Should().Be("Dog");
    }

    [Test]
    public async Task CreateAsync_StampsExplicitTypeDiscriminatorOverride_WhenConfigured()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        const string explicitDiscriminator = "CustomAnimal";
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator(explicitDiscriminator));

        var partitionKey = NewId();
        var id = NewId();

        // Model.PrepareCreate's own default resolution (nameof(AnimalItem)) must be overridden by the explicit WithTypeDiscriminator value configured above - not left as the default.
        var created = await animals.CreateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Dog" });
        created.Value.TypeDiscriminator.Should().Be(explicitDiscriminator);

        // The persisted document must also be retrievable via this same container - i.e. it must not have been stamped with the default value that CheckModel would then reject.
        var fetched = await animals.GetAsync(CompositeKey.Create(id), partitionKey);
        fetched.Should().NotBeNull();
        fetched!.TypeDiscriminator.Should().Be(explicitDiscriminator);

        var queried = await animals.Query(q => q.Where(m => m.PartitionKey == partitionKey)).ToListAsync();
        queried.Should().ContainSingle();
        queried[0].TypeDiscriminator.Should().Be(explicitDiscriminator);
    }

    [Test]
    public async Task UpdateAsync_StampsExplicitTypeDiscriminatorOverride_WhenConfigured()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var cosmosDb = CreateCosmosDb();
        const string explicitDiscriminator = "CustomAnimal";
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminator(explicitDiscriminator));

        var partitionKey = NewId();
        var id = NewId();
        await animals.CreateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Dog" });

        // An update round-trip must also re-stamp the explicit override, not Model.PrepareUpdate's own default.
        var updated = await animals.UpdateAsync(new AnimalItem { Id = id, PartitionKey = partitionKey, Name = "Puppy" });
        updated.Value.TypeDiscriminator.Should().Be(explicitDiscriminator);

        var fetched = await animals.GetAsync(CompositeKey.Create(id), partitionKey);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Puppy");
        fetched.TypeDiscriminator.Should().Be(explicitDiscriminator);
    }
}
