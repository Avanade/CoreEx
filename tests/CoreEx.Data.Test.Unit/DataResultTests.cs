namespace CoreEx.Data.Test.Unit;

public class DataResultTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Map_TransformsValueOnceAndPreservesMutationFlag(bool wasMutated)
    {
        var source = new DataResult<int>(42, wasMutated);
        var calls = 0;

        DataResult<string> mapped = source.Map(value =>
        {
            calls++;
            return value.ToString();
        });

        mapped.Value.Should().Be("42");
        mapped.WasMutated.Should().Be(wasMutated);
        calls.Should().Be(1);
        source.Value.Should().Be(42);
        source.WasMutated.Should().Be(wasMutated);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Map_NullDelegate_Throws(bool wasMutated)
    {
        var source = new DataResult<int>(42, wasMutated);

        Action action = () => source.Map<string>(null!); // Intentionally violate the non-null delegate contract.

        action.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Map_MapperFailure_PropagatesUnchanged(bool wasMutated)
    {
        var source = new DataResult<int>(42, wasMutated);
        var error = new InvalidOperationException("Mapping failed.");

        Action action = () => source.Map<string>(_ => throw error);

        action.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(error);
    }

    [Test]
    public void Map_NullSource_IsPassedToMapper()
    {
        var source = new DataResult<string?>(null, false);

        var mapped = source.Map(value => value is null ? "missing" : "present");

        mapped.Value.Should().Be("missing");
        mapped.WasMutated.Should().BeFalse();
    }

    [Test]
    public void Map_NullDestination_IsPreserved()
    {
        var source = new DataResult<int>(42, false);

        DataResult<string?> mapped = source.Map<string?>(_ => null);

        mapped.Value.Should().BeNull();
        mapped.WasMutated.Should().BeFalse();
    }

    [Test]
    public void Map_DefaultResult_MapsDefaultValueWithoutMutation()
    {
        DataResult<int> source = default;

        var mapped = source.Map(value => value + 1);

        mapped.Value.Should().Be(1);
        mapped.WasMutated.Should().BeFalse();
    }
}
