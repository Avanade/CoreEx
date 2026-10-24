namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class OptionalJsonConverterTests
{
    [Test]
    public void ContainingObject_OmitsUnspecifiedAndPreservesSpecifiedNullAndDefault()
    {
        var options = new CoreEx.Json.JsonDefaults.JsonDefaultConfiguration().SerializerOptions;
        var target = new OptionalDocument
        {
            Name = Optional<string?>.Specified(null),
            Count = 0
        };

        JsonSerializer.Serialize(target, options).Should().Be("""{"name":null,"count":0}""");
    }

    [Test]
    public void ContainingObject_DeserializationDistinguishesMissingNullAndDefault()
    {
        var options = new CoreEx.Json.JsonDefaults.JsonDefaultConfiguration().SerializerOptions;
        var target = JsonSerializer.Deserialize<OptionalDocument>("""{"name":null,"count":0}""", options);

        target.Should().NotBeNull();
        target!.Name.IsSpecified.Should().BeTrue();
        target.Name.Value.Should().BeNull();
        target.Count.IsSpecified.Should().BeTrue();
        target.Count.Value.Should().Be(0);
        target.Description.IsSpecified.Should().BeFalse();
    }

    [Test]
    public void Converter_UsesInnerScalarRepresentation()
    {
        var options = new CoreEx.Json.JsonDefaults.JsonDefaultConfiguration().SerializerOptions;

        JsonSerializer.Serialize(Optional<int>.Specified(12), options).Should().Be("12");
        JsonSerializer.Deserialize<Optional<string?>>("null", options).Should().Be(Optional<string?>.Specified(null));
    }

    public class OptionalDocument
    {
        public Optional<string?> Name { get; set; }
        public Optional<int> Count { get; set; }
        public Optional<string?> Description { get; set; }
    }
}
