namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies that two distinct business model types (<see cref="AnimalItem"/> and <see cref="PlantItem"/>) can safely share the same container/partition using
/// <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminatorFilter(string?)"/> - no envelope/wrapper type required.
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
        var animals = cosmosDb.Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminatorFilter());
        var plants = cosmosDb.Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminatorFilter());

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
}
