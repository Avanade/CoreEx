namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Verifies <see cref="CosmosDbBatch.ImportDiscriminatedBatchAsync"/> does not corrupt the caller-owned <see cref="JsonDataReaderOptions.Properties"/> it temporarily mutates to stamp each
/// group's <see cref="ITypeDiscriminator.TypeDiscriminator"/> value.
/// </summary>
[TestFixture]
public class CosmosDbBatchTests : CosmosTestBase
{
    private const string ContainerId = "discriminated-batch-items";

    private const string Yaml = """
        discriminated-batch-items:
          - Animal:
              - { id: ^guid, partitionKey: batch-pk, name: Dog }
          - Plant:
              - { id: ^guid, partitionKey: batch-pk, name: Fern }
        """;

    /// <summary>
    /// A caller who has already set the '<c>typeDiscriminator</c>' property (e.g. via its own <see cref="JsonDataReaderOptions.RootNodePreProcessor"/> or a prior, unrelated import) must get that
    /// exact value back afterward - not have it silently wiped, which would corrupt any further reuse of the same <see cref="JsonDataReaderOptions"/> for other, unrelated seeding.
    /// </summary>
    [Test]
    public async Task ImportDiscriminatedBatchAsync_PreExistingTypeDiscriminatorProperty_IsRestoredAfterwards()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var options = new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase);
        options.Properties["typeDiscriminator"] = "pre-existing-value";

        var jdr = JsonDataReader.ParseYaml(Yaml, options);

        await CosmosDbBatch.ImportDiscriminatedBatchAsync(TestDatabase, jdr).ConfigureAwait(false);

        options.Properties["typeDiscriminator"].Should().Be("pre-existing-value", "the caller's own pre-existing property value must be restored, not left cleared or overwritten by the last-processed discriminator");
    }

    /// <summary>
    /// Where the caller had not set the '<c>typeDiscriminator</c>' property at all, it must be absent again afterward - the pre-fix behavior of an unconditional removal happened to get this
    /// case right, so this is a regression guard rather than a reproduction of the original bug.
    /// </summary>
    [Test]
    public async Task ImportDiscriminatedBatchAsync_NoPreExistingTypeDiscriminatorProperty_LeavesPropertyAbsentAfterwards()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);

        var options = new JsonDataReaderOptions(JsonPropertyNamingConvention.CamelCase);
        var jdr = JsonDataReader.ParseYaml(Yaml, options);

        await CosmosDbBatch.ImportDiscriminatedBatchAsync(TestDatabase, jdr).ConfigureAwait(false);

        options.Properties.Should().NotContainKey("typeDiscriminator");
    }
}
