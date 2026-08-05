using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class ETagTests
{
    private sealed class Etaggable(string? etag) : IReadOnlyETag
    {
        public string? ETag { get; } = etag;
    }

    [Test]
    public void TryCompare_Strings_MatchAndMismatch()
    {
        ETag.TryCompare("abc", "abc").Should().BeTrue();
        ETag.TryCompare("abc", "def").Should().BeFalse();
        ETag.TryCompare((string?)null, (string?)null).Should().BeTrue();
        ETag.TryCompare("abc", null).Should().BeFalse();
    }

    [Test]
    public void TryCompare_ReadOnlyETag_DelegatesToStringOverload()
    {
        ETag.TryCompare(new Etaggable("abc"), new Etaggable("abc")).Should().BeTrue();
        ETag.TryCompare(new Etaggable("abc"), new Etaggable("def")).Should().BeFalse();
        ETag.TryCompare((IReadOnlyETag?)null, (IReadOnlyETag?)null).Should().BeTrue();
    }

    [Test]
    public void Compare_Strings_Match_DoesNotThrow()
    {
        Action act = () => ETag.Compare("abc", "abc");
        act.Should().NotThrow();
    }

    [Test]
    public void Compare_Strings_Mismatch_ThrowsConcurrencyException()
    {
        Action act = () => ETag.Compare("abc", "def");
        act.Should().Throw<ConcurrencyException>();
    }

    [Test]
    public void Compare_Mismatch_InvokesAdjuster()
    {
        var adjusted = false;
        Action act = () => ETag.Compare("abc", "def", adjuster: _ => adjusted = true);
        act.Should().Throw<ConcurrencyException>();
        adjusted.Should().BeTrue();
    }

    [Test]
    public void Compare_ReadOnlyETag_DelegatesToStringOverload()
    {
        Action act = () => ETag.Compare(new Etaggable("abc"), new Etaggable("def"));
        act.Should().Throw<ConcurrencyException>();
    }

    [Test]
    public void CompareWithResult_Match_ReturnsSuccess()
    {
        var result = ETag.CompareWithResult("abc", "abc");
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CompareWithResult_Mismatch_ReturnsConcurrencyError()
    {
        var result = ETag.CompareWithResult("abc", "def");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
    }

    [Test]
    public void CompareWithResult_ReadOnlyETag_DelegatesToStringOverload()
    {
        var result = ETag.CompareWithResult(new Etaggable("abc"), new Etaggable("abc"));
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void FormatETag_Null_ReturnsNull() => ETag.FormatETag(null).Should().BeNull();

    [Test]
    public void FormatETag_AlreadyQuoted_ReturnsUnchanged() => ETag.FormatETag("\"abc\"").Should().Be("\"abc\"");

    [Test]
    public void FormatETag_WeakPrefixed_StripsWeakPrefix() => ETag.FormatETag("W/\"abc\"").Should().Be("\"abc\"");

    [Test]
    public void FormatETag_PlainValue_AddsQuotes() => ETag.FormatETag("abc").Should().Be("\"abc\"");

    [Test]
    public void ParseETag_Null_ReturnsNull() => ETag.ParseETag((string?)null).Should().BeNull();

    [Test]
    public void ParseETag_Empty_ReturnsEmpty() => ETag.ParseETag(string.Empty).Should().Be(string.Empty);

    [Test]
    public void ParseETag_Quoted_StripsQuotes() => ETag.ParseETag("\"abc\"").Should().Be("abc");

    [Test]
    public void ParseETag_WeakPrefixed_StripsWeakPrefixAndQuotes() => ETag.ParseETag("W/\"abc\"").Should().Be("abc");

    [Test]
    public void ParseETag_Unquoted_ReturnsUnchanged() => ETag.ParseETag("abc").Should().Be("abc");

    [Test]
    public void Generate_NullValue_ReturnsNull() => ETag.Generate<string>(null).Should().BeNull();

    [Test]
    public void Generate_Value_ReturnsTwelveCharHash()
    {
        var etag = ETag.Generate(new { Id = 1, Name = "test" });
        etag.Should().NotBeNullOrEmpty();
        etag!.Length.Should().Be(12);
    }

    [Test]
    public void Generate_SameValue_ReturnsSameHash()
    {
        var etag1 = ETag.Generate(new { Id = 1, Name = "test" });
        var etag2 = ETag.Generate(new { Id = 1, Name = "test" });
        etag1.Should().Be(etag2);
    }

    [Test]
    public void Generate_DifferentParts_ReturnsDifferentHash()
    {
        var etag1 = ETag.Generate(new { Id = 1 }, parts: ["a"]);
        var etag2 = ETag.Generate(new { Id = 1 }, parts: ["b"]);
        etag1.Should().NotBe(etag2);
    }

    [Test]
    public void Generate_Parts_NullOrEmpty_ReturnsNull()
    {
        ETag.Generate((string[])null!).Should().BeNull();
        ETag.Generate().Should().BeNull();
    }

    [Test]
    public void Generate_Parts_SinglePart_ReturnsTwelveCharHash()
    {
        var etag = ETag.Generate("abc");
        etag.Should().NotBeNullOrEmpty();
        etag!.Length.Should().Be(12);
    }

    [Test]
    public void Generate_Parts_MultipleParts_ReturnsConsistentHash()
    {
        var etag1 = ETag.Generate("abc", "def");
        var etag2 = ETag.Generate("abc", "def");
        etag1.Should().Be(etag2);
    }

    [Test]
    public void Generate_Parts_DifferentOrder_ReturnsDifferentHash()
    {
        var etag1 = ETag.Generate("abc", "def");
        var etag2 = ETag.Generate("def", "abc");
        etag1.Should().NotBe(etag2);
    }
}
