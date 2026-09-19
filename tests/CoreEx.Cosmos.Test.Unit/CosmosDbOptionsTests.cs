namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies <see cref="CosmosDbOptions"/> caches <see cref="CosmosDbModelOptions{TModel}"/> per <c>(containerId, TModel)</c> pair, not per <paramref name="containerId"/> alone - a container is legitimately
/// shared by multiple distinct model types (see <see cref="CosmosDbModelOptions{TModel}.WithTypeDiscriminator(string?)"/>), so keying by <c>containerId</c> alone would let the first <c>TModel</c>
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

    // Regression test for a review-flagged bug: CosmosDbOptions is typically a long-lived singleton shared across every CosmosDb instance (e.g. one per request/scope). CosmosDb.Container<TModel>
    // previously called GetOrAddModelOptions<TModel>(containerId) THEN unconditionally invoked its own configure callback against whatever was returned - including an already-configured, shared
    // instance from a prior CosmosDb instance/scope - so a configure callback appending state (e.g. WithFilter) kept accumulating duplicate registrations for as long as the process ran. Moving
    // construction+configure inside GetOrAddModelOptions's own GetOrAdd factory ensures configure only ever runs once-ever per (containerId, TModel), regardless of how many separate callers ask.
    [Test]
    public void GetOrAddModelOptions_WithConfigure_OnlyInvokedOnce_AcrossMultipleCallsForSameKey()
    {
        var options = new CosmosDbOptions();
        var invocationCount = 0;

        var first = options.GetOrAddModelOptions<AnimalItem>("shared-container", o => { invocationCount++; o.WithFilter(q => q); });
        invocationCount.Should().Be(1);

        // Simulates a second CosmosDb instance (e.g. a new request/scope) sharing this same CosmosDbOptions and requesting the same container/model again with its own configure callback.
        var second = options.GetOrAddModelOptions<AnimalItem>("shared-container", o => { invocationCount++; o.WithFilter(q => q); });

        second.Should().BeSameAs(first);
        invocationCount.Should().Be(1, "configure must only be invoked the first time CosmosDbModelOptions<TModel> is created for a given (containerId, TModel) - never re-invoked against an already-shared instance.");
    }

    [Test]
    public void GetOrAddModelOptions_WithoutConfigure_DoesNotThrow()
    {
        var options = new CosmosDbOptions();

        var modelOptions = options.GetOrAddModelOptions<AnimalItem>("no-configure-container");

        modelOptions.Should().NotBeNull();
        options.GetOrAddModelOptions<AnimalItem>("no-configure-container").Should().BeSameAs(modelOptions);
    }

    // End-to-end version of the regression above through the public entry point a consumer actually calls (CosmosDb.Container<TModel>), proving the fix holds across genuinely separate CosmosDb
    // instances (each with its own empty _modelContainers cache) that merely happen to share one CosmosDbOptions - exactly the singleton-options/scoped-CosmosDb topology the bug affected.
    [Test]
    public void Container_WithConfigure_SharedOptionsAcrossMultipleCosmosDbInstances_OnlyInvokesConfigureOnce()
    {
        // A CosmosClient can be constructed, and Container() called on it, without any network I/O - GetDatabase/GetContainer are client-side reference factories only.
        var client = new CosmosClient("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
        var sharedOptions = new CosmosDbOptions();
        var invocationCount = 0;

        var firstCosmosDb = new CosmosDb(client, "test-db", sharedOptions);
        var firstContainer = firstCosmosDb.Container<AnimalItem>("shared-container", o => { invocationCount++; o.WithFilter(q => q); });
        invocationCount.Should().Be(1);

        // A brand-new CosmosDb instance - as would happen per request/scope in a real app - has its own empty _modelContainers cache, but shares the same (singleton-style) CosmosDbOptions.
        var secondCosmosDb = new CosmosDb(client, "test-db", sharedOptions);
        var secondContainer = secondCosmosDb.Container<AnimalItem>("shared-container", o => { invocationCount++; o.WithFilter(q => q); });

        invocationCount.Should().Be(1, "configure must not be re-invoked just because a new CosmosDb instance/scope is the first to request an already-configured, shared model options instance.");
        secondContainer.Options.Should().BeSameAs(firstContainer.Options);
    }
}
