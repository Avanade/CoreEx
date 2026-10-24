namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class WrappedValueCleanerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void TypedOptionalCleaning_DoesNotBoxWrappers(bool specified)
    {
        var value = specified ? Optional<string>.Specified("text") : default;
        for (var i = 0; i < 1000; i++)
            value = Cleaner.Clean(value);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
            value = Cleaner.Clean(value);

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        value.IsSpecified.Should().Be(specified);
        if (specified)
            value.Value.Should().Be("text");
    }

    [Test]
    public void TypedCustomStructCleaning_DoesNotBoxWrappers()
    {
        var value = new TextValue("text");
        for (var i = 0; i < 1000; i++)
            value = RuntimeMetadata.Clean(value);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
            value = RuntimeMetadata.Clean(value);

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.Should().Be(0);
        value.Value.Should().Be("text");
    }

    [Test]
    public void ErasedWrapperCleaning_PreservesRuntimeDispatch()
    {
        object value = new TextValue("text  ");
        CoreEx.Entities.Abstractions.IWrappedValue<string?> reference = new TextReference("text  ");
        var cleaned = RuntimeMetadata.Clean(value);
        var cleanedReference = RuntimeMetadata.Clean(reference);

        cleaned.Should().BeOfType<TextValue>().Which.Value.Should().Be("text");
        cleanedReference.Should().BeOfType<TextReference>().Which.Value.Should().Be("text");
        RuntimeMetadata.Clean<TextReference?>(null).Should().BeNull();
    }

    [TestCase(null)]
    [TestCase("  text  ")]
    public void NonOptionalWrappers_CleanDirectlyAndWriteBackToProperties(string? text)
    {
        var entity = new Container { StructValue = new(text), ReferenceValue = new(text) };
        var originalReference = entity.ReferenceValue;
        var directStruct = Cleaner.Clean(entity.StructValue);
        var directReference = Cleaner.Clean(entity.ReferenceValue);

        RuntimeMetadata.Clean(entity);

        var expected = Cleaner.Clean(text);
        directStruct.Value.Should().Be(expected);
        directReference.Should().NotBeNull();
        directReference!.Value.Should().Be(expected);
        entity.StructValue.Value.Should().Be(expected);
        entity.ReferenceValue.Value.Should().Be(expected);
        entity.ReferenceValue.Should().NotBeSameAs(originalReference);
        originalReference.Value.Should().Be(text);
    }

    public readonly record struct TextValue(string? Value)
        : CoreEx.Entities.Abstractions.IWrappedValue<string?>, CoreEx.Entities.Abstractions.IWrappedValueCleaner<TextValue>
    {
        TextValue CoreEx.Entities.Abstractions.IWrappedValueCleaner<TextValue>.Clean(CleanArgs args)
            => new TextValue(RuntimeMetadata.Clean(Value, args));
    }

    public sealed record TextReference(string? Value)
        : CoreEx.Entities.Abstractions.IWrappedValue<string?>, CoreEx.Entities.Abstractions.IWrappedValueCleaner<TextReference>
    {
        TextReference CoreEx.Entities.Abstractions.IWrappedValueCleaner<TextReference>.Clean(CleanArgs args)
            => new TextReference(RuntimeMetadata.Clean(Value, args));
    }

    public class Container
    {
        public TextValue StructValue { get; set; }
        public TextReference ReferenceValue { get; set; } = new(null);
    }
}
