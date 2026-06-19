using CoreEx.Results;
using CoreEx.Validation;

namespace CoreEx.Test.Unit.Validation;

[TestFixture]
public class ValidationTests
{
    [TearDown]
    public void TearDown()
    {
        // Reset static properties to defaults before each test to avoid side effects.
        CoreEx.Validation.Validation.MandatoryFormat = new("CoreEx.Validation.MandatoryFormat", "{0} is required.");
        CoreEx.Validation.Validation.ValueName = "value";
        CoreEx.Validation.Validation.ValueText = new("CoreEx.Validation.ValueText", "Value");
    }

    [Test]
    public void MandatoryFormat_StaticProperty()
    {
        CoreEx.Validation.Validation.MandatoryFormat = new("custom", "custom required");
        CoreEx.Validation.Validation.MandatoryFormat.KeyAndOrText.Should().Be("custom");
        CoreEx.Validation.Validation.MandatoryFormat.FallbackText.Should().Be("custom required");
    }

    [Test]
    public void ValueNameDefault_StaticProperty()
    {
        CoreEx.Validation.Validation.ValueName = "foo";
        CoreEx.Validation.Validation.ValueName.Should().Be("foo");
    }

    [Test]
    public void ValueTextDefault_StaticProperty()
    {
        CoreEx.Validation.Validation.ValueText = new("bar", "Bar");
        CoreEx.Validation.Validation.ValueText.KeyAndOrText.Should().Be("bar");
        CoreEx.Validation.Validation.ValueText.FallbackText.Should().Be("Bar");
    }

    [Test]
    public void Required_NonDefault_ReturnsValue()
    {
        var value = 123;
        var result = value.Required();
        result.Should().Be(123);
    }

    [Test]
    public void Required_Default_ThrowsValidationException()
    {
        int value = 0;
        Action act = () => value.Required();
        act.Should().Throw<ValidationException>();
    }

    [Test]
    public void Required_CustomNameAndText()
    {
        int value = 0;
        Action act = () => value.Required("myValue", new("custom", "Custom"));
        var ex = act.Should().Throw<ValidationException>()
            .WithMessage("A data validation error occurred.")
            .Which.Messages.Should().ContainSingle().Which.ToString().Should().Be("MessageItem { Type = Error, Text = Custom is required., Property = myValue }");
    }

    [Test]
    public void Requires_ResultT_NonDefault_ReturnsResult()
    {
        var result = Result.Ok(5).Requires(5, "myValue");
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Requires_ResultT_Default_ReturnsFailure()
    {
        var result = Result.Ok(0).Requires(0, "myValue");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void Requires_ResultT_CustomText()
    {
        var result = Result.Ok(0).Requires(0, "myValue", new("custom", "Custom"));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var vex = (ValidationException)result.Error;
        vex.Message.Should().Be("A data validation error occurred.");
        vex.Messages.Should().ContainSingle().Which.ToString().Should().Be("MessageItem { Type = Error, Text = Custom is required., Property = myValue }");
    }

    [Test]
    public void Requires_ResultT_Func_NonDefault_ReturnsResult()
    {
        var result = Result.Ok(5).Requires(() => 5, "myValue");
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Requires_ResultT_Func_Default_ReturnsFailure()
    {
        var result = Result.Ok(0).Requires(() => 0, "myValue");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public void Requires_ResultT_Func_CustomText()
    {
        var result = Result.Ok(0).Requires(() => 0, "myValue", "Custom");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        var vex = (ValidationException)result.Error;
        vex.Message.Should().Be("A data validation error occurred.");
        vex.Messages.Should().ContainSingle().Which.ToString().Should().Be("MessageItem { Type = Error, Text = Custom is required., Property = myValue }");
    }

    [Test]
    public void Requires_ResultT_Func_Null_Throws()
    {
        var result = Result.Ok(1);
        Action act = () => result.Requires((Func<int>)null!, "myValue");
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Requires_ResultT_Func_EmptyName_Throws()
    {
        var result = Result.Ok(1);
        Action act = () => result.Requires(() => 1, "");
        act.Should().Throw<ArgumentException>();
    }
}