namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class OptionalTests
{
    [Test]
    public void Default_IsUnspecified_AndValueThrows()
    {
        var value = default(Optional<int>);

        value.IsSpecified.Should().BeFalse();
        Action act = () => _ = value.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ImplicitConversion_MarksDefaultAndNullValuesSpecified()
    {
        Optional<int> number = 0;
        Optional<string?> text = null;

        number.IsSpecified.Should().BeTrue();
        number.Value.Should().Be(0);
        text.IsSpecified.Should().BeTrue();
        text.Value.Should().BeNull();
    }

    [Test]
    public void Equality_IncludesSpecifiedState()
    {
        Optional<string?> unspecified = default;
        Optional<string?> specifiedNull = Optional<string?>.Specified(null);

        unspecified.Should().NotBe(specifiedNull);
        specifiedNull.Should().Be(Optional<string?>.Specified(null));
        Optional<int>.Specified(0).Should().NotBe(default(Optional<int>));
    }

    [Test]
    public void Clean_CleansSpecifiedInnerValue_AndLeavesUnspecifiedUntouched()
    {
        var entity = new OptionalContainer { Value = Optional<string?>.Specified("  text  ") };
        RuntimeMetadata.Clean(entity);
        var unspecified = Cleaner.Clean(default(Optional<string?>));

        entity.Value.IsSpecified.Should().BeTrue();
        entity.Value.Value.Should().Be("  text");
        unspecified.IsSpecified.Should().BeFalse();
    }

    public class OptionalContainer
    {
        public Optional<string?> Value { get; set; }
    }

    [TestCase(false, null)]
    [TestCase(true, null)]
    [TestCase(true, "  text  ")]
    public void CustomCleaner_ParticipatesInDirectAndPropertyCleaning(bool specified, string? text)
    {
        var value = new CustomOptional(specified, text);
        var direct = RuntimeMetadata.Clean(value);
        var entity = new CustomContainer { Value = value };
        RuntimeMetadata.Clean(entity);

        direct.IsSpecified.Should().Be(specified);
        entity.Value.IsSpecified.Should().Be(specified);
        if (specified)
        {
            direct.Value.Should().Be(text is null ? null : "  text");
            entity.Value.Value.Should().Be(direct.Value);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CustomCleaner_RejectsInvalidReconstruction(bool returnNull)
    {
        Action clean = () => RuntimeMetadata.Clean(new InvalidOptionalCleaner(returnNull));
        clean.Should().Throw<InvalidOperationException>()
            .WithMessage("*must return a non-null value of the same runtime wrapper type.");
    }

    public readonly record struct CustomOptional(bool IsSpecified, string? InnerValue)
        : CoreEx.Entities.Abstractions.IOptionalValue<string?>, CoreEx.Entities.Abstractions.IWrappedValueCleaner<CustomOptional>
    {
        public string? Value => IsSpecified ? InnerValue : throw new InvalidOperationException("An unspecified value must not be read.");

        CustomOptional CoreEx.Entities.Abstractions.IWrappedValueCleaner<CustomOptional>.Clean(CleanArgs args)
            => IsSpecified ? new CustomOptional(true, RuntimeMetadata.Clean(Value, args)) : this;
    }

    public class CustomContainer
    {
        public CustomOptional Value { get; set; }
    }

    private class InvalidOptionalCleaner(bool returnNull) : CoreEx.Entities.Abstractions.IWrappedValueCleaner<InvalidOptionalCleaner>
    {
        // Deliberately violate the non-null return contract to verify runtime enforcement.
        public InvalidOptionalCleaner Clean(CleanArgs args) => returnNull ? null! : new DifferentWrapper();
    }

    private sealed class DifferentWrapper() : InvalidOptionalCleaner(false);
}
