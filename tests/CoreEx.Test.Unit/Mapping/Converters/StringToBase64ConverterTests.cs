using CoreEx.Mapping.Converters;

namespace CoreEx.Test.Unit.Mapping.Converters;

[TestFixture]
public class StringToBase64ConverterTests
{
    private readonly StringBase64Converter _converter = StringBase64Converter.Default;

    [Test]
    public void ConvertToDestination_ValidBase64String_ReturnsBytes()
    {
        var text = "Hello, World!";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        var result = _converter.ConvertToDestination(base64);

        result.Should().NotBeNull();
        System.Text.Encoding.UTF8.GetString(result!).Should().Be(text);
    }

    [Test]
    public void ConvertToDestination_Null_ReturnsNull()
    {
        _converter.ConvertToDestination((string?)null).Should().BeNull();
    }

    [Test]
    public void ConvertToSource_ValidBytes_ReturnsBase64String()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("Test123");
        var result = _converter.ConvertToSource(bytes);

        result.Should().Be(Convert.ToBase64String(bytes));
    }

    [Test]
    public void ConvertToSource_Null_ReturnsNull()
    {
        _converter.ConvertToSource((byte[]?)null).Should().BeNull();
    }

    [Test]
    public void RoundTrip_StringToBytesAndBack()
    {
        var original = "RoundTrip!";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(original));
        var bytes = _converter.ConvertToDestination(base64);
        var roundTrip = _converter.ConvertToSource(bytes);

        roundTrip.Should().Be(base64);
    }

    [Test]
    public void ConvertToDestination_InvalidBase64_ThrowsFormatException()
    {
        Action act = () => _converter.ConvertToDestination("not_base64!");
        act.Should().Throw<FormatException>();
    }
}