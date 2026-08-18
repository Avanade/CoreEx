namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies that two distinct business model types (<see cref="AnimalItem"/> and <see cref="PlantItem"/>) can safely share the same container/partition using
/// <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminatorFilter(string?)"/> - no envelope/wrapper type required.
/// </summary>
[TestFixture]
public class CosmosDbContainerTypeDiscriminatorTests : CosmosTestBase
{
    private const string ContainerId = "discriminator-items";

    private static async Task<CosmosDbContainer<AnimalItem>> GetAnimalsAsync()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        return CreateCosmosDb().Container<AnimalItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminatorFilter());
    }

    private static async Task<CosmosDbContainer<PlantItem>> GetPlantsAsync()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        return CreateCosmosDb().Container<PlantItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey).WithTypeDiscriminatorFilter());
    }

    [Test]
    public async Task Query_OnlyReturnsMatchingTypeDiscriminator_WhenTypesShareContainerAndPartition()
    {
        var animals = await GetAnimalsAsync();
        var plants = await GetPlantsAsync();

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
