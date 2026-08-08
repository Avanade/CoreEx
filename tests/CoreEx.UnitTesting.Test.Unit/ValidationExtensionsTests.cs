using CoreEx.Entities;
using CoreEx.Validation;

namespace CoreEx.UnitTesting.Test.Unit;

public class ValidationExtensionsTests
{
    public class Widget
    {
        public string? Name { get; set; }
    }

    public class WidgetValidator : Validator<Widget>
    {
        public static readonly WidgetValidator Default = new();

        public WidgetValidator() => Property(w => w.Name).Mandatory();
    }

    [Test]
    public void AssertSuccess_Passes_WhenValid()
        => WidgetValidator.Default.AssertSuccess(new Widget { Name = "Sprocket" });

    [Test]
    public async Task AssertSuccessAsync_Passes_WhenValid()
        => await WidgetValidator.Default.AssertSuccessAsync(new Widget { Name = "Sprocket" });

    [Test]
    public void AssertErrors_Passes_WhenExpectedErrorMatches()
        => WidgetValidator.Default.AssertErrors(new Widget(), ("name", "Name is required."));

    [Test]
    public void AssertErrors_Fails_WhenExpectedErrorDoesNotMatch()
    {
        Action act = () => WidgetValidator.Default.AssertErrors(new Widget(), ("name", "Some other message."));
        act.Should().Throw<Exception>();
    }

    [Test]
    public void AssertSuccess_Fails_WhenInvalid()
    {
        Action act = () => WidgetValidator.Default.AssertSuccess(new Widget());
        act.Should().Throw<Exception>();
    }

    // Regression: ValidationException.AssertErrors renders a null MessageItem.Text as "none" (matching IValidator<T>.AssertErrors'
    // own x.Text?.ToString() ?? "none" convention) — previously the AddAssertErrorsExtension wiring rendered it as "" instead.
    [Test]
    public void ValidationExceptionAssertErrors_NullText_RendersAsNone()
    {
        var vex = new ValidationException(new MessageItem { Type = MessageType.Error, Property = "name", Text = null });

        vex.AssertErrors(("name", "none"));
    }
}
