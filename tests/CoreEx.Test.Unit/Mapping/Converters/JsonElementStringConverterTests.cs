using System.Text.Json;
using CoreEx.Mapping.Converters;

namespace CoreEx.Test.Unit.Mapping.Converters;

[TestFixture]
public class JsonElementStringConverterTests
{
    private readonly JsonElementStringConverter _converter = JsonElementStringConverter.Default;

    [Test]
    public void ConvertToDestination_Value_ReturnsJson()
    {
        using var doc = JsonDocument.Parse("""{"name":"Bob","number":42}""");
        var result = _converter.ConvertToDestination(doc.RootElement);

        result.Should().Be("""{"name":"Bob","number":42}""");
    }

    [Test]
    public void ConvertToDestination_Null_ReturnsNull()
    {
        _converter.ConvertToDestination((JsonElement?)null).Should().BeNull();
    }

    [Test]
    public void ConvertToSource_Json_ReturnsJsonElement()
    {
        var result = _converter.ConvertToSource("""{"name":"Bob","number":42}""");

        result!.Value.GetProperty("name").GetString().Should().Be("Bob");
        result.Value.GetProperty("number").GetInt32().Should().Be(42);
    }

    [Test]
    public void ConvertToSource_Null_ReturnsNull()
    {
        _converter.ConvertToSource((string?)null).Should().BeNull();
    }

    [Test]
    public void RoundTrip_JsonElementToStringAndBack()
    {
        using var doc = JsonDocument.Parse("""{"name":"Alice","number":7}""");
        var json = _converter.ConvertToDestination(doc.RootElement);
        var roundTrip = _converter.ConvertToSource(json);

        roundTrip!.Value.GetProperty("name").GetString().Should().Be("Alice");
        roundTrip.Value.GetProperty("number").GetInt32().Should().Be(7);
    }

    // The following two tests exercise the non-generic IConverter object-based overloads directly, which is
    // where a copy/paste bug (issue #175) previously caused an InvalidCastException or infinite recursion
    // (StackOverflowException) because the casts were against the wrong side's type (TDestination instead of
    // TSource, and vice versa).
    [Test]
    public void IConverter_ConvertToDestination_Object_ReturnsJson()
    {
        IConverter converter = _converter;
        using var doc = JsonDocument.Parse("""{"name":"Bob","number":42}""");

        var result = converter.ConvertToDestination(doc.RootElement);

        result.Should().Be("""{"name":"Bob","number":42}""");
    }

    [Test]
    public void IConverter_ConvertToSource_Object_ReturnsJsonElement()
    {
        IConverter converter = _converter;

        var result = (JsonElement?)converter.ConvertToSource("""{"name":"Bob","number":42}""");

        result!.Value.GetProperty("name").GetString().Should().Be("Bob");
        result.Value.GetProperty("number").GetInt32().Should().Be(42);
    }
}
