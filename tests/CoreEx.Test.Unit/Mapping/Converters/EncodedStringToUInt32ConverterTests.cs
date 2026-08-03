using CoreEx.Mapping.Converters;

namespace CoreEx.Test.Unit.Mapping.Converters;

[TestFixture]
public class EncodedStringToUInt32ConverterTests
{
    private readonly EncodedStringToUInt32Converter _converter = EncodedStringToUInt32Converter.Default;

    [Test]
    public void ConvertToDestination_ValidBase64String_ReturnsUInt32()
    {
        var value = 12345u;
        var base64 = Convert.ToBase64String(BitConverter.GetBytes(value));

        _converter.ConvertToDestination(base64).Should().Be(value);
    }

    [Test]
    public void ConvertToDestination_Null_ReturnsZero()
    {
        _converter.ConvertToDestination((string?)null).Should().Be(0u);
    }

    [Test]
    public void ConvertToSource_ValidUInt32_ReturnsBase64String()
    {
        var value = 98765u;

        var result = _converter.ConvertToSource(value);

        result.Should().Be(Convert.ToBase64String(BitConverter.GetBytes(value)));
    }

    [Test]
    public void ConvertToSource_Zero_ReturnsNull()
    {
        _converter.ConvertToSource(0u).Should().BeNull();
    }

    [Test]
    public void RoundTrip_UInt32ToBase64AndBack()
    {
        var value = 555u;
        var base64 = _converter.ConvertToSource(value);
        var roundTrip = _converter.ConvertToDestination(base64);

        roundTrip.Should().Be(value);
    }

    // The following two tests exercise the non-generic IConverter object-based overloads directly (see issue #175,
    // which found the exact same defect class in JsonElementStringConverter's non-generic overloads).
    [Test]
    public void IConverter_ConvertToDestination_Object_ReturnsUInt32()
    {
        IConverter converter = _converter;
        var value = 4242u;
        var base64 = Convert.ToBase64String(BitConverter.GetBytes(value));

        var result = converter.ConvertToDestination(base64);

        result.Should().Be(value);
    }

    [Test]
    public void IConverter_ConvertToSource_Object_ReturnsBase64String()
    {
        IConverter converter = _converter;
        var value = 111u;

        var result = converter.ConvertToSource(value);

        result.Should().Be(Convert.ToBase64String(BitConverter.GetBytes(value)));
    }
}
