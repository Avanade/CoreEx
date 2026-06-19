using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class CleanerTests
{
    [SetUp]
    public void SetUp() => Cleaner.ResetDefaults();

    [Test]
    public void Clean_String_Trim_End()
    {
        var input = "abc   ";
        var result = Cleaner.Clean(input, StringTrim.End, StringTransform.None, StringCase.None);
        result.Should().Be("abc");
    }

    [Test]
    public void Clean_String_Trim_Both()
    {
        var input = "  abc  ";
        var result = Cleaner.Clean(input, StringTrim.Both, StringTransform.None, StringCase.None);
        result.Should().Be("abc");
    }

    [Test]
    public void Clean_String_Trim_Start()
    {
        var input = "  abc";
        var result = Cleaner.Clean(input, StringTrim.Start, StringTransform.None, StringCase.None);
        result.Should().Be("abc");
    }

    [Test]
    public void Clean_String_Transform_EmptyToNull()
    {
        var input = "";
        var result = Cleaner.Clean(input, StringTrim.None, StringTransform.EmptyToNull, StringCase.None);
        result.Should().BeNull();
    }

    [Test]
    public void Clean_String_Transform_NullToEmpty()
    {
        string? input = null;
        var result = Cleaner.Clean(input, StringTrim.None, StringTransform.NullToEmpty, StringCase.None);
        result.Should().Be(string.Empty);
    }

    [Test]
    public void Clean_String_Case_Lower()
    {
        var input = "ABC";
        var result = Cleaner.Clean(input, StringTrim.None, StringTransform.None, StringCase.Lower);
        result.Should().Be("abc");
    }

    [Test]
    public void Clean_String_Case_Upper()
    {
        var input = "abc";
        var result = Cleaner.Clean(input, StringTrim.None, StringTransform.None, StringCase.Upper);
        result.Should().Be("ABC");
    }

    [Test]
    public void Clean_String_Case_Title()
    {
        var input = "a blue carrot";
        var result = Cleaner.Clean(input, StringTrim.None, StringTransform.None, StringCase.Title);
        result.Should().Be("A Blue Carrot");
    }

    [Test]
    public void Clean_String_Null()
    {
        Cleaner.DefaultStringTrim = StringTrim.Both;
        Cleaner.DefaultStringTransform = StringTransform.EmptyToNull;
        Cleaner.DefaultStringCase = StringCase.Upper;

        string? input = null;
        var result = Cleaner.Clean(input);
        result.Should().BeNull();
    }

    [Test]
    public void Clean_String_UseDefault_Values()
    {
        Cleaner.DefaultStringTrim = StringTrim.Both;
        Cleaner.DefaultStringTransform = StringTransform.EmptyToNull;
        Cleaner.DefaultStringCase = StringCase.Upper;

        var input = "  abc  ";
        var result = Cleaner.Clean(input);
        result.Should().Be("ABC");
    }

    [Test]
    public void Clean_DateTime_Transform_Utc()
    {
        var local = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(local, DateTimeTransform.DateTimeUtc);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Clean_DateTime_Transform_Local()
    {
        var utc = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var result = Cleaner.Clean(utc, DateTimeTransform.DateTimeLocal);
        result.Kind.Should().Be(DateTimeKind.Local);
    }

    [Test]
    public void Clean_DateTime_Transform_DateOnly()
    {
        var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(dt, DateTimeTransform.DateOnly);
        result.Date.Should().Be(dt.Date);
        result.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Test]
    public void Clean_DateTime_Transform_Unspecified()
    {
        var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(dt, DateTimeTransform.DateTimeUnspecified);
        result.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Test]
    public void Clean_NullableDateTime_Null()
    {
        DateTime? dt = null;
        var result = Cleaner.Clean(dt, DateTimeTransform.DateTimeUtc);
        result.Should().BeNull();
    }

    [Test]
    public void Clean_NullableDateTime_Value()
    {
        DateTime? dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(dt, DateTimeTransform.DateTimeUtc);
        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Clean_Generic_String()
    {
        var input = "  abc  ";
        var result = Cleaner.Clean(input);
        result.Should().Be("  abc");
    }

    [Test]
    public void Clean_Generic_DateTime()
    {
        var dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(dt);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Clean_Generic_DateTime_Nullable()
    {
        DateTime? dt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var result = Cleaner.Clean(dt);
        result.Should().NotBeNull();
        result.Value.Kind.Should().Be(DateTimeKind.Utc);

        dt = null;
        result = Cleaner.Clean(dt);
        result.Should().BeNull();
    }

    [Test]
    public void Clean_Generic_Null()
    {
        object? value = null;
        var result = Cleaner.Clean(value);
        result.Should().BeNull();
    }

    [Test]
    public void Clean_Generic_ICollection()
    {
        List<string>? input = [];
        var result = Cleaner.Clean(input);
        result.Should().BeNull();

        List<string> input2 = [];
        var result2 = Cleaner.Clean(input2);
        result2.Should().BeNull();  // Don't be fooled by lack of nullability declaration (compiler sugar only) - an empty collection is always cleaned to null.

        input = ["Abc"];
        result = Cleaner.Clean(input);
        result.Should().NotBeNull();
    }

    [Test]
    public void DefaultStringTrim_SetToUseDefault_Throws()
    {
        Action act = () => Cleaner.DefaultStringTrim = StringTrim.UseDefault;
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DefaultStringTransform_SetToUseDefault_Throws()
    {
        Action act = () => Cleaner.DefaultStringTransform = StringTransform.UseDefault;
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DefaultStringCase_SetToUseDefault_Throws()
    {
        Action act = () => Cleaner.DefaultStringCase = StringCase.UseDefault;
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DefaultDateTimeTransform_SetToUseDefault_Throws()
    {
        Action act = () => Cleaner.DefaultDateTimeTransform = DateTimeTransform.UseDefault;
        act.Should().Throw<ArgumentException>();
    }
}