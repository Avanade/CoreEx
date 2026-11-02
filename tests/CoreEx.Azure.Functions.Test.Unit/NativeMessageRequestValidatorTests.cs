namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class NativeMessageRequestValidatorTests
{
    [TestCase(null)]
    [TestCase("")]
    public async Task MissingOutcome_IsRequired(string? outcome)
    {
        var result = await NativeMessageRequestValidator.Default.ValidateAsync(new NativeMessageRequest(outcome));
        result.HasErrors.Should().BeTrue();
        result.ToResult().Error.Should().BeOfType<CoreEx.ValidationException>();
    }

    [Test]
    public async Task SpecifiedOutcome_IsValid()
        => (await NativeMessageRequestValidator.Default.ValidateAsync(new NativeMessageRequest("success"))).HasErrors.Should().BeFalse();
}
