namespace CoreEx.Validation.Test.Unit.Rules;

[TestFixture]
public class UnwrapRuleTests
{
    [Test]
    public void WrapperExtensions_ExposeOnlyWrapperSpecificReceivers()
    {
        var methods = typeof(ValidationExtensions).GetMethods()
            .Where(method => method.Name is "Unwrap" or "Specified")
            .ToArray();

        methods.Should().HaveCount(6);
        foreach (var method in methods)
        {
            var propertyType = method.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            var isExtension = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
            isExtension.Should().Be(!propertyType.IsGenericParameter);
        }
    }

    [Test]
    public void Unwrap_InfersCustomReferenceWrapperValueType()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.WithText("Wrapped text").Unwrap().String(3));

        validator.ValidateAsSuccess(new Request { Text = new TextWrapper("abc") });
        validator.ValidateAsError(new Request { Text = new TextWrapper("long") }, "text", "Wrapped text must not exceed 3 character(s) in length.");
    }

    [Test]
    public void Unwrap_SupportsCustomStructWrappers()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Number, rule => ValidationExtensions.Unwrap<Request, NumberWrapper, int>(rule).Between(1, 3));

        validator.ValidateAsSuccess(new Request { Number = new NumberWrapper(2) });
        validator.ValidateAsError(new Request { Number = new NumberWrapper(4) }, "number", "Number must be between '1' and '3'.");
    }

    [Test]
    public void Unwrap_CustomWrapperCannotBeRebuiltByNormalization()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.Text, rule => rule.Unwrap().CompareValues(["ABC"], StringComparer.OrdinalIgnoreCase, overrideValueWhereMatched: true));
        var request = new Request { Text = new TextWrapper("abc") };

        Assert.ThrowsAsync<InvalidOperationException>(async () => await validator.ValidateAsync(request))
            .Message.Should().Be("The property 'Text' is read-only and cannot be overridden.");
        request.Text.Value.Should().Be("abc");
    }

    [Test]
    public void Unwrap_CustomOptionalChecksPresenceBeforeReadingValue()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.OptionalText, rule => rule.Unwrap().Mandatory());

        validator.ValidateAsSuccess(new Request());
        validator.ValidateAsSuccess(new Request { OptionalText = new OptionalTextWrapper(true, "abc") });
        validator.ValidateAsError(new Request { OptionalText = new OptionalTextWrapper(true, null) }, "optionalText", "Optional text is required.");
    }

    [Test]
    public void Specified_ValidatesCustomOptionalPresenceWithoutReadingValue()
    {
        var validator = Validator.Create<Request>()
            .HasProperty(x => x.OptionalText, rule => rule.Specified());

        validator.ValidateAsError(new Request(), "optionalText", "Optional text is required.");
        validator.ValidateAsSuccess(new Request { OptionalText = new OptionalTextWrapper(true, null) });
    }

    public class Request
    {
        public TextWrapper Text { get; set; } = new("abc");
        public NumberWrapper Number { get; set; }
        public OptionalTextWrapper OptionalText { get; set; } = new(false, null);
    }

    public sealed record TextWrapper(string Value) : CoreEx.Entities.Abstractions.IWrappedValue<string>;

    public readonly record struct NumberWrapper(int Value) : CoreEx.Entities.Abstractions.IWrappedValue<int>;

    public sealed class OptionalTextWrapper(bool isSpecified, string? value) : CoreEx.Entities.Abstractions.IOptionalValue<string?>
    {
        public bool IsSpecified => isSpecified;
        string? CoreEx.Entities.Abstractions.IWrappedValue<string?>.Value => isSpecified ? value : throw new InvalidOperationException("An unspecified value must not be read.");
    }
}
