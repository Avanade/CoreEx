using CoreEx.Database.Extended;
using CoreEx.Wildcards;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class DatabaseWildcardTests
{
    [Test]
    public void Replace_DefaultWildcard_ConvertsMultiWildcardOnly() => new DatabaseWildcard().Replace("abc*").Should().Be("abc%");

    [Test]
    public void Replace_BothAll_ConvertsMultiAndSingleWildcards() => new DatabaseWildcard(Wildcard.BothAll).Replace("a*b?c").Should().Be("a%b_c");

    [Test]
    public void Replace_BothAll_EscapesLiteralWildcardCharacters() => new DatabaseWildcard(Wildcard.BothAll).Replace("a%b_c").Should().Be("a[%]b[_]c");

    [Test]
    public void Replace_CustomDatabaseWildcardCharacters_AreUsedInsteadOfDefaults()
        => new DatabaseWildcard(Wildcard.BothAll, multiWildcard: '#', singleWildcard: '!').Replace("a*b?c").Should().Be("a#b!c");

    [Test]
    public void Replace_MatchAllSelection_ReturnsSingleMultiWildcardCharacter() => new DatabaseWildcard(Wildcard.MultiAll).Replace("*").Should().Be("%");

    [Test]
    public void Constructor_SameMultiAndSingleWildcardCharacter_Throws()
    {
        Action act = () => new DatabaseWildcard(Wildcard.BothAll, multiWildcard: '%', singleWildcard: '%');
        act.Should().Throw<ArgumentException>().WithParameterName("multiWildcard");
    }

    [Test]
    public void Constructor_UnsupportedSingleWildcard_WithoutCharacter_Throws()
    {
        Action act = () => new DatabaseWildcard(Wildcard.BothAll, singleWildcard: char.MinValue);
        act.Should().Throw<ArgumentException>();
    }
}
