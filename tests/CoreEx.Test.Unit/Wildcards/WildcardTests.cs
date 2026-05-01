using CoreEx.Wildcards;

namespace CoreEx.Test.Unit.Wildcards;

[TestFixture]
public class WildcardTests
{
    [Test]
    public void StaticProperties_AreCorrect()
    {
        Wildcard.None.SupportedSelection.Should().Be(WildcardSelection.None | WildcardSelection.Equal);
        Wildcard.MultiBasic.SupportedSelection.Should().Be(WildcardSelection.MultiBasic);
        Wildcard.MultiAll.SupportedSelection.Should().Be(WildcardSelection.MultiAll);
        Wildcard.BothAll.SupportedSelection.Should().Be(WildcardSelection.BothAll);

        var oldDefault = Wildcard.Default;
        Wildcard.Default = Wildcard.BothAll;
        Wildcard.Default.Should().Be(Wildcard.BothAll);
        Wildcard.Default = oldDefault;
    }

    [Test]
    public void Constructor_ValidConfig_SetsProperties()
    {
        var charsNotAllowed = new[] { '!', '@' };
        var wc = new Wildcard(WildcardSelection.MultiAll, '*', '?', charsNotAllowed, WildcardSpaceTreatment.Compress);
        wc.SupportedSelection.Should().Be(WildcardSelection.MultiAll);
        wc.MultiWildcard.Should().Be('*');
        wc.SingleWildcard.Should().Be('?');
        wc.CharactersNotAllowed.Should().BeEquivalentTo(charsNotAllowed);
        wc.SpaceTreatment.Should().Be(WildcardSpaceTreatment.Compress);
    }

    [Test]
    public void Constructor_InvalidConfig_Throws()
    {
#pragma warning disable CA1806 // Do not ignore method results
        Action a1 = () => new Wildcard(WildcardSelection.Undetermined);
        a1.Should().Throw<ArgumentException>();

        Action a2 = () => new Wildcard(WildcardSelection.InvalidCharacter);
        a2.Should().Throw<ArgumentException>();

        Action a3 = () => new Wildcard(WildcardSelection.MultiWildcard, '*', '*');
        a3.Should().Throw<ArgumentException>();

        Action a4 = () => new Wildcard(WildcardSelection.MultiWildcard, '*', '?', ['*']);
        a4.Should().Throw<ArgumentException>();

        Action a5 = () => new Wildcard(WildcardSelection.SingleWildcard, '*', char.MinValue);
        a5.Should().Throw<ArgumentException>();

        Action a6 = () => new Wildcard(WildcardSelection.MultiWildcard, char.MinValue, '?');
        a6.Should().Throw<ArgumentException>();
#pragma warning restore CA1806 // Do not ignore method results
    }

    [Test]
    public void Parse_NullOrEmpty_ReturnsNone()
    {
        var wc = Wildcard.MultiBasic;
        var result = wc.Parse(null);
        result.Selection.Should().Be(WildcardSelection.None);
        result.Text.Should().BeNull();

        result = wc.Parse("");
        result.Selection.Should().Be(WildcardSelection.None);
        result.Text.Should().Be("");
    }

    [Test]
    public void Parse_InvalidPatterns()
    {
        var wc = Wildcard.MultiBasic;
        wc.Parse("a*b").HasError.Should().BeTrue();
        wc.Parse("?bc").HasError.Should().BeTrue();
        wc.Parse("a?c").HasError.Should().BeTrue();
        wc.Parse("ab?").HasError.Should().BeTrue();
    }

    [Test]
    public void Parse_ValidPatterns()
    {
        var wc = Wildcard.BothAll;
        var r1 = wc.Parse("abc");
        r1.Selection.Should().HaveFlag(WildcardSelection.Equal);
        r1.Text.Should().Be("abc");
        r1.CreateRegex().ToString().Should().Be("^abc$");

        var r2 = wc.Parse("*abc");
        r2.Selection.Should().HaveFlag(WildcardSelection.EndsWith);
        r2.Selection.Should().HaveFlag(WildcardSelection.MultiWildcard);
        r2.Text.Should().Be("*abc");
        r2.CreateRegex().ToString().Should().Be("^.*abc$");

        var r3 = wc.Parse("abc*");
        r3.Selection.Should().HaveFlag(WildcardSelection.StartsWith);
        r3.Selection.Should().HaveFlag(WildcardSelection.MultiWildcard);
        r3.Text.Should().Be("abc*");
        r3.CreateRegex().ToString().Should().Be("^abc.*$");

        var r4 = wc.Parse("a*c");
        r4.Selection.Should().HaveFlag(WildcardSelection.Embedded);
        r4.Selection.Should().HaveFlag(WildcardSelection.MultiWildcard);
        r4.Text.Should().Be("a*c");
        r4.CreateRegex().ToString().Should().Be("^a.*c$");

        var r5 = wc.Parse("a**c");
        r5.Selection.Should().HaveFlag(WildcardSelection.Embedded);
        r5.Selection.Should().HaveFlag(WildcardSelection.MultiWildcard);
        r5.Selection.Should().HaveFlag(WildcardSelection.AdjacentWildcards);
        r5.Text.Should().Be("a*c");
        r5.CreateRegex().ToString().Should().Be("^a.*c$");

        var r6 = wc.Parse("a?c");
        r6.Selection.Should().HaveFlag(WildcardSelection.Embedded);
        r6.Selection.Should().HaveFlag(WildcardSelection.SingleWildcard);
        r6.Text.Should().Be("a?c");
        r6.CreateRegex().ToString().Should().Be("^a.c$");
    }

    [Test]
    public void Parse_InvalidCharacters()
    {
        var wc = new Wildcard(WildcardSelection.BothAll, '*', '?', ['!']);
        var result = wc.Parse("a!b");
        result.Selection.Should().HaveFlag(WildcardSelection.InvalidCharacter);
        result.HasError.Should().BeTrue();
    }
}