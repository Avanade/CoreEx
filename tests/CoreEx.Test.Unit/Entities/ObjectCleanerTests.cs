namespace CoreEx.Test.Unit.Entities;

[TestFixture]
[NonParallelizable]
public partial class ObjectCleanerTests
{
    [SetUp]
    public void SetUp() => Cleaner.ResetDefaults();

    [TearDown]
    public void TearDown() => Cleaner.ResetDefaults();

    [TestCase(false, "hello ", "hello")]
    [TestCase(true, "hello ", "hello")]
    [TestCase(false, " hello ", " hello")]
    [TestCase(true, " hello ", " hello")]
    [TestCase(false, " ", null)]
    [TestCase(true, " ", null)]
    [TestCase(false, "", null)]
    [TestCase(true, "", null)]
    [TestCase(false, null, null)]
    [TestCase(true, null, null)]
    [TestCase(false, "hello", "hello")]
    [TestCase(true, "hello", "hello")]
    public void Clean_WritesBackOrdinaryStringProperties(bool generated, string? input, string? expected)
    {
        var value = CreateProbe(generated);
        value.Text = input;

        var result = Cleaner.Clean(value);

        value.Text.Should().Be(expected);
        result.Should().BeSameAs(value);
    }

    [TestCase(false, StringCase.Upper, "HELLO WORLD")]
    [TestCase(true, StringCase.Upper, "HELLO WORLD")]
    [TestCase(false, StringCase.Lower, "hello world")]
    [TestCase(true, StringCase.Lower, "hello world")]
    [TestCase(false, StringCase.Title, "Hello WORLD")]
    [TestCase(true, StringCase.Title, "Hello WORLD")]
    public void Clean_WritesBackConfiguredStringCasing(bool generated, StringCase casing, string expected)
    {
        Cleaner.DefaultStringCase = casing;
        var value = CreateProbe(generated);
        value.Text = "hello WORLD ";

        Cleaner.Clean(value);

        value.Text.Should().Be(expected);
    }

    [TestCase(false, DateTimeTransform.UseDefault)]
    [TestCase(true, DateTimeTransform.UseDefault)]
    [TestCase(false, DateTimeTransform.DateTimeUtc)]
    [TestCase(true, DateTimeTransform.DateTimeUtc)]
    [TestCase(false, DateTimeTransform.DateTimeLocal)]
    [TestCase(true, DateTimeTransform.DateTimeLocal)]
    [TestCase(false, DateTimeTransform.DateTimeUnspecified)]
    [TestCase(true, DateTimeTransform.DateTimeUnspecified)]
    [TestCase(false, DateTimeTransform.DateOnly)]
    [TestCase(true, DateTimeTransform.DateOnly)]
    public void Clean_WritesBackDateTimeAndNullableDateTime(bool generated, DateTimeTransform transform)
    {
        if (transform != DateTimeTransform.UseDefault)
            Cleaner.DefaultDateTimeTransform = transform;

        var kind = transform is DateTimeTransform.DateTimeUnspecified or DateTimeTransform.DateOnly ? DateTimeKind.Utc : DateTimeKind.Unspecified;
        var timestamp = new DateTime(2024, 1, 1, 12, 30, 0, kind);
        var expected = Cleaner.Clean(timestamp, transform);
        var value = CreateProbe(generated);
        value.Timestamp = timestamp;
        value.NullableTimestamp = timestamp;

        Cleaner.Clean(value);

        value.Timestamp.Ticks.Should().Be(expected.Ticks);
        value.Timestamp.Kind.Should().Be(expected.Kind);
        value.NullableTimestamp.Should().NotBeNull();
        value.NullableTimestamp.GetValueOrDefault().Ticks.Should().Be(expected.Ticks);
        value.NullableTimestamp.GetValueOrDefault().Kind.Should().Be(expected.Kind);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Clean_NoneLeavesPropertiesUnchanged(bool generated)
    {
        Cleaner.DefaultCleanOption = CleanOption.None;
        var value = CreateProbe(generated);
        value.Text = " ";
        value.Timestamp = new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Unspecified);

        Cleaner.Clean(value);

        value.Text.Should().Be(" ");
        value.Timestamp.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Clean_RespectsTypeSpecificExclusions(bool generated)
    {
        var added = Cleaner.CleanOptions.TryAdd(typeof(string), CleanOption.None);
        added.Should().BeTrue();
        try
        {
            var value = CreateProbe(generated);
            value.Text = " ";

            Cleaner.Clean(value);

            value.Text.Should().Be(" ");
        }
        finally
        {
            Cleaner.CleanOptions.TryRemove(typeof(string), out _);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Clean_LeavesReadOnlyPropertiesUnchanged(bool generated)
    {
        var value = CreateProbe(generated);

        Cleaner.Clean(value);

        value.ReadOnlyText.Should().Be("hello ");
    }

    [Test]
    public void Clean_RespectsGeneratedPropertyExclusions()
    {
        var value = new DefaultProbe { Excluded = " ", Ignored = " " };

        Cleaner.Clean(value);

        value.Excluded.Should().Be(" ");
        value.Ignored.Should().Be(" ");
    }

    [Test]
    public void Clean_PreservesConfiguredCollectionDefaultsAndWritesBackNullStrings()
    {
        var value = new DefaultProbe { Text = " " };
        var values = value.Values;

        Cleaner.Clean(value);

        value.Text.Should().BeNull();
        value.Values.Should().BeSameAs(values).And.BeEmpty();
    }

    [Test]
    public void Clean_PreservesNestedCleanAndDefaultSemantics()
    {
        var defaulted = new ChangeLog { CreatedBy = "" };
        var retained = new ChangeLog { CreatedBy = "" };
        var value = new DefaultProbe { ChangeLog = defaulted, RetainedChangeLog = retained };

        Cleaner.Clean(value);

        value.ChangeLog.Should().BeSameAs(defaulted);
        value.RetainedChangeLog.Should().BeSameAs(retained);
        defaulted.IsDefault().Should().BeTrue();
        retained.IsDefault().Should().BeTrue();

        Cleaner.Clean(value, new CleanArgs { CleanAndDefaultNested = true });

        value.ChangeLog.Should().BeNull();
        value.RetainedChangeLog.Should().BeSameAs(retained);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Clean_CleansNestedObjectsWithoutReassigningGraphProperties(bool generated)
    {
        var child = CreateProbe(generated);
        child.Text = "hello ";
        var parent = new SetterProbe<IProbe>(child);
        var list = new SetterProbe<List<IProbe>>([child]);
        var array = new SetterProbe<IProbe[]>([child]);
        var dictionary = new SetterProbe<Dictionary<string, IProbe>>(new() { ["child"] = child });

        object[] containers = [parent, list, array, dictionary];
        foreach (var container in containers)
        {
            child.Text = "hello ";
            Cleaner.Clean(container);
            child.Text.Should().Be("hello");
        }

        parent.Value.Should().BeSameAs(child);
        list.Value[0].Should().BeSameAs(child);
        array.Value[0].Should().BeSameAs(child);
        dictionary.Value["child"].Should().BeSameAs(child);
        parent.SetterCalls.Should().Be(0);
        list.SetterCalls.Should().Be(0);
        array.SetterCalls.Should().Be(0);
        dictionary.SetterCalls.Should().Be(0);
    }

    [TestCase(StringCase.None, "hello")]
    [TestCase(StringCase.Upper, "HELLO")]
    public void Clean_DoesNotReassignUnchangedStrings(StringCase casing, string expected)
    {
        Cleaner.DefaultStringCase = casing;
        var value = new SetterProbe<string?>("hello ");

        Cleaner.Clean(value);
        Cleaner.Clean(value);

        value.Value.Should().Be(expected);
        value.SetterCalls.Should().Be(1);
    }

    [Test]
    public void Clean_DoesNotReassignUnchangedDateTimesOrOtherValueTypes()
    {
        var date = new SetterProbe<DateTime>(new(2024, 1, 1, 12, 30, 0, DateTimeKind.Unspecified));
        var number = new SetterProbe<int>(7);

        Cleaner.Clean(date);
        Cleaner.Clean(date);
        Cleaner.Clean(number);

        date.Value.Kind.Should().Be(DateTimeKind.Utc);
        date.SetterCalls.Should().Be(1);
        number.Value.Should().Be(7);
        number.SetterCalls.Should().Be(0);
    }

    [Test]
    public void Clean_WritesBackTypeErasedScalarProperties()
    {
        var text = new SetterProbe<object>("hello ");
        var date = new SetterProbe<object>(new DateTime(2024, 1, 1, 12, 30, 0, DateTimeKind.Unspecified));

        Cleaner.Clean(text);
        Cleaner.Clean(date);

        text.Value.Should().Be("hello");
        date.Value.Should().BeOfType<DateTime>().Which.Kind.Should().Be(DateTimeKind.Utc);
    }

    private static IProbe CreateProbe(bool generated) => generated ? new GeneratedProbe() : new PocoProbe();

    private interface IProbe
    {
        string? Text { get; set; }

        DateTime Timestamp { get; set; }

        DateTime? NullableTimestamp { get; set; }

        string ReadOnlyText { get; }
    }

    private sealed class PocoProbe : IProbe
    {
        public string? Text { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? NullableTimestamp { get; set; }

        public string ReadOnlyText => "hello ";
    }

    [Contract]
    private partial class GeneratedProbe : IProbe
    {
        public string? Text { get; set; }

        public DateTime Timestamp { get; set; }

        public DateTime? NullableTimestamp { get; set; }

        public string ReadOnlyText => "hello ";
    }

    [Contract]
    private partial class DefaultProbe
    {
        public string? Text { get; set; } = "seed";

        public string[] Values { get; set; } = [];

        public ChangeLog? ChangeLog { get; set; }

        [Clean(CleanOption.Clean)]
        public ChangeLog? RetainedChangeLog { get; set; }

        [Clean(CleanOption.None)]
        public string? Excluded { get; set; }

        [ContractIgnore]
        public string? Ignored { get; set; }
    }

    private sealed class SetterProbe<T>(T value)
    {
        private T _value = value;
        private int _setterCalls;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                _setterCalls++;
            }
        }

        public int SetterCalls => _setterCalls;
    }
}
