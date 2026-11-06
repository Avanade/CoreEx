namespace CoreEx.Test.Unit;

[TestFixture]
public class ArgumentGuardExtensionsTests
{
    [Test]
    public void ThrowIfNullOrWhiteSpace_NullThrowsWithInferredParameterName()
    {
        string? value = null;
        Action action = () => value.ThrowIfNullOrWhiteSpace();

        action.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be(nameof(value));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t\r\n")]
    [TestCase("\u00a0")]
    public void ThrowIfNullOrWhiteSpace_BlankThrowsWithInferredParameterName(string value)
    {
        Action action = () => value.ThrowIfNullOrWhiteSpace();

        action.Should().ThrowExactly<ArgumentException>().Which.ParamName.Should().Be(nameof(value));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void ThrowIfNullOrWhiteSpace_HonorsExplicitParameterName(string? value)
    {
        Action action = () => value.ThrowIfNullOrWhiteSpace("source");

        action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("source");
    }

    [TestCase("value")]
    [TestCase(" value ")]
    [TestCase("\tvalue\r\n")]
    public void ThrowIfNullOrWhiteSpace_ReturnsOriginalNonNullableValue(string? value)
    {
        string result = value.ThrowIfNullOrWhiteSpace();

        result.Should().BeSameAs(value);
        result.Length.Should().Be(value.Length);
    }
}
