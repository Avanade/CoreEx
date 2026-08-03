using CoreEx.Mapping.Converters;

namespace CoreEx.Test.Unit.Mapping.Converters;

[TestFixture]
public class TypeToJsonStringConverterTests
{
    private sealed record TestValue(string Name, int Number);

    private readonly TypeToJsonStringConverter<TestValue> _converter = TypeToJsonStringConverter<TestValue>.Default;

    [Test]
    public void ConvertToDestination_Value_ReturnsJson()
    {
        var value = new TestValue("Bob", 42);
        var result = _converter.ConvertToDestination(value);

        result.Should().Be("""{"name":"Bob","number":42}""");
    }

    [Test]
    public void ConvertToDestination_Null_ReturnsNull()
    {
        // T is unconstrained (no nullable annotation), but the underlying ValueConverter explicitly handles a null source at runtime.
        _converter.ConvertToDestination(default(TestValue)!).Should().BeNull();
    }

    [Test]
    public void ConvertToSource_Json_ReturnsValue()
    {
        var result = _converter.ConvertToSource("""{"name":"Bob","number":42}""");

        result.Should().Be(new TestValue("Bob", 42));
    }

    [Test]
    public void ConvertToSource_Null_ReturnsDefault()
    {
        _converter.ConvertToSource((string?)null).Should().BeNull();
    }

    [Test]
    public void RoundTrip_ValueToJsonAndBack()
    {
        var value = new TestValue("Alice", 7);
        var json = _converter.ConvertToDestination(value);
        var roundTrip = _converter.ConvertToSource(json);

        roundTrip.Should().Be(value);
    }

    // The following two tests exercise the non-generic IConverter object-based overloads directly, which is
    // where a copy/paste bug previously caused either an InvalidCastException or infinite recursion (StackOverflowException)
    // because the casts were against the wrong side's type (TDestination instead of TSource, and vice versa).
    [Test]
    public void IConverter_ConvertToDestination_Object_ReturnsJson()
    {
        IConverter converter = _converter;
        var value = new TestValue("Bob", 42);

        var result = converter.ConvertToDestination(value);

        result.Should().Be("""{"name":"Bob","number":42}""");
    }

    [Test]
    public void IConverter_ConvertToSource_Object_ReturnsValue()
    {
        IConverter converter = _converter;

        var result = converter.ConvertToSource("""{"name":"Bob","number":42}""");

        result.Should().Be(new TestValue("Bob", 42));
    }
}
