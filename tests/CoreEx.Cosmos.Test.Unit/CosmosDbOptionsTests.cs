namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies <see cref="CosmosDbOptions"/> caches <see cref="CosmosDbModelOptions{TModel}"/> per <c>(containerId, TModel)</c> pair, not per <paramref name="containerId"/> alone - a container is legitimately
/// shared by multiple distinct model types (see <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminatorFilter(string?)"/>), so keying by <c>containerId</c> alone would let the first <c>TModel</c>
/// registered for a given <c>containerId</c> "win" the cache slot - for the lifetime of this (typically singleton) instance - with every other type sharing that <c>containerId</c> throwing
/// <see cref="InvalidCastException"/> when it tries to cast the cached entry back to its own <see cref="CosmosDbModelOptions{TModel}"/>.
/// </summary>
[TestFixture]
public class CosmosDbOptionsTests
{
    [Test]
    public void GetOrAddModelOptions_DifferentModelTypes_SameContainerId_ReturnsDistinctInstances()
    {
        var options = new CosmosDbOptions();

        var animalOptions = options.GetOrAddModelOptions<AnimalItem>("shared-container");
        var plantOptions = options.GetOrAddModelOptions<PlantItem>("shared-container");

        animalOptions.Should().NotBeNull();
        plantOptions.Should().NotBeNull();

        // Re-fetching returns the SAME cached instance per type (proves the cache still works correctly, just now correctly scoped per-type rather than per-containerId-alone).
        options.GetOrAddModelOptions<AnimalItem>("shared-container").Should().BeSameAs(animalOptions);
        options.GetOrAddModelOptions<PlantItem>("shared-container").Should().BeSameAs(plantOptions);
    }

    [Test]
    public void TryGetModelOptions_DifferentModelTypes_SameContainerId_EachResolvesItsOwn()
    {
        var options = new CosmosDbOptions();
        var animalOptions = options.GetOrAddModelOptions<AnimalItem>("shared-container");
        var plantOptions = options.GetOrAddModelOptions<PlantItem>("shared-container");

        options.TryGetModelOptions<AnimalItem>("shared-container", out var foundAnimalOptions).Should().BeTrue();
        foundAnimalOptions.Should().BeSameAs(animalOptions);

        options.TryGetModelOptions<PlantItem>("shared-container", out var foundPlantOptions).Should().BeTrue();
        foundPlantOptions.Should().BeSameAs(plantOptions);
    }

    [Test]
    public void TryGetModelOptions_NotRegistered_ReturnsFalse()
    {
        var options = new CosmosDbOptions();

        options.TryGetModelOptions<AnimalItem>("never-registered", out var modelOptions).Should().BeFalse();
        modelOptions.Should().BeNull();
    }
}
