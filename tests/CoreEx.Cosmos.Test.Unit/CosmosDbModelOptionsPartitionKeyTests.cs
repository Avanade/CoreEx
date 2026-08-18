namespace CoreEx.Cosmos.Test.Unit;

/// <summary>
/// Pure unit tests for <see cref="CosmosDbModelOptions{TModel}"/>'s <c>WithPartitionKey</c>/<c>WithFixedPartitionKey</c> resolution and validation logic - deliberately does not derive from
/// <see cref="CosmosTestBase"/>, as none of this requires a live Cosmos DB endpoint.
/// </summary>
[TestFixture]
public class CosmosDbModelOptionsPartitionKeyTests
{
    [Test]
    public void WithFixedPartitionKey_ThenWithPartitionKey_Throws()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("shared");
        Assert.Throws<InvalidOperationException>(() => options.WithPartitionKey(m => m.PartitionKey));
    }

    [Test]
    public void WithPartitionKey_ThenWithFixedPartitionKey_Throws()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithPartitionKey(m => m.PartitionKey);
        Assert.Throws<InvalidOperationException>(() => options.WithFixedPartitionKey("shared"));
    }

    [Test]
    public void GetPartitionKey_Model_FixedConfigured_ModelValueNull_UsesFixed()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("shared");
        var model = new TestItem { Id = "id1", Name = "X" }; // PartitionKey left null.

        options.GetPartitionKey(model).Should().Be(new PartitionKey("shared"));
        model.PartitionKey.Should().Be("shared"); // Written back onto the model - required for Cosmos DB to accept the write.
    }

    [Test]
    public void GetPartitionKey_Model_FixedConfigured_ModelValueMatches_UsesFixed()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("shared");
        var model = new TestItem { Id = "id1", PartitionKey = "shared", Name = "X" };

        options.GetPartitionKey(model).Should().Be(new PartitionKey("shared"));
    }

    [Test]
    public void GetPartitionKey_Model_FixedConfigured_ModelValueDiffers_Throws()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("shared");
        var model = new TestItem { Id = "id1", PartitionKey = "different", Name = "X" };

        Assert.Throws<InvalidOperationException>(() => options.GetPartitionKey(model));
    }

    [Test]
    public void GetPartitionKey_Model_FuncConfigured_ModelValueDiffers_Throws()
    {
        // The func always resolves to "computed" regardless of the model - the model's own (different) value should be flagged, not silently ignored.
        var options = new CosmosDbModelOptions<TestItem>().WithPartitionKey(_ => "computed");
        var model = new TestItem { Id = "id1", PartitionKey = "different", Name = "X" };

        Assert.Throws<InvalidOperationException>(() => options.GetPartitionKey(model));
    }

    [Test]
    public void GetPartitionKey_Model_FuncConfigured_WritesResolvedValueBackOntoModel()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithPartitionKey(_ => "computed");
        var model = new TestItem { Id = "id1", Name = "X" }; // PartitionKey left null.

        options.GetPartitionKey(model).Should().Be(new PartitionKey("computed"));
        model.PartitionKey.Should().Be("computed");
    }

    [Test]
    public void GetPartitionKey_Model_NoOverrideConfigured_FallsBackToModelValue()
    {
        var options = new CosmosDbModelOptions<TestItem>();
        var model = new TestItem { Id = "id1", PartitionKey = "own-value", Name = "X" };

        options.GetPartitionKey(model).Should().Be(new PartitionKey("own-value"));
    }

    [Test]
    public void GetPartitionKey_Explicit_ReturnsExplicitValue_IgnoringFixed()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("fixed");

        options.GetPartitionKey(new PartitionKey("explicit")).Should().Be(new PartitionKey("explicit"));
    }

    [Test]
    public void GetPartitionKey_Explicit_Null_FallsBackToFixed()
    {
        var options = new CosmosDbModelOptions<TestItem>().WithFixedPartitionKey("fixed");

        options.GetPartitionKey((PartitionKey?)null).Should().Be(new PartitionKey("fixed"));
    }

    [Test]
    public void GetPartitionKey_Explicit_NullAndNoFixedConfigured_Throws()
    {
        var options = new CosmosDbModelOptions<TestItem>();

        Assert.Throws<InvalidOperationException>(() => options.GetPartitionKey((PartitionKey?)null));
    }
}
