using CoreEx.Localization;

namespace CoreEx.Test.Unit.Localization;

[TestFixture]
public class LTextTests
{
    [Test]
    public void Empty_Static_IsEmpty()
    {
        LText.Empty.IsEmpty.Should().BeTrue();
        LText.Empty.KeyAndOrText.Should().BeNull();
        LText.Empty.FallbackText.Should().BeNull();
        LText.Empty.Args.Should().BeNull();
    }

    [Test]
    public void Constructor_KeyOnly()
    {
        var ltext = new LText("key1");
        ltext.KeyAndOrText.Should().Be("key1");
        ltext.FallbackText.Should().BeNull();
        ltext.Args.Should().BeNull();
        ltext.IsEmpty.Should().BeFalse();
        ltext.WasFallBackTextSetToNull.Should().BeFalse();
    }

    [Test]
    public void Constructor_KeyFallbackArgs()
    {
        var ltext = new LText("key2", "fallback", 1, "x");
        ltext.KeyAndOrText.Should().Be("key2");
        ltext.FallbackText.Should().Be("fallback");
        ltext.Args.Should().BeEquivalentTo(new object?[] { 1, "x" });
        ltext.IsEmpty.Should().BeFalse();
        ltext.WasFallBackTextSetToNull.Should().BeFalse();
    }

    [Test]
    public void Constructor_KeyNullFallbackNull_SetsWasFallbackTextSetToNull()
    {
        var ltext = new LText("key3", null, 5);
        ltext.KeyAndOrText.Should().Be("key3");
        ltext.FallbackText.Should().BeNull();
        ltext.Args.Should().BeEquivalentTo(new object?[] { 5 });
        ltext.WasFallBackTextSetToNull.Should().BeTrue();
    }

    [Test]
    public void WithArgs_AppendsArgs()
    {
        var ltext = new LText("k", "f", 1);
        var ltext2 = ltext.WithArgs(2, 3);
        ltext2.KeyAndOrText.Should().Be("k");
        ltext2.FallbackText.Should().Be("f");
        ltext2.Args.Should().BeEquivalentTo(new object?[] { 1, 2, 3 });
        ltext2.WasFallBackTextSetToNull.Should().BeFalse();
    }

    [Test]
    public void WithArgs_NullOrEmpty_ReturnsSelf()
    {
        var ltext = new LText("k", "f", 1);
        ltext.WithArgs().Should().Be(ltext);
        ltext.WithArgs(null!).Should().Be(ltext);
    }

    [Test]
    public void EnsureNoArgs_NoArgs_ReturnsSelf()
    {
        var ltext = new LText("k");
        ltext.EnsureNoArgs().Should().Be(ltext);
    }

    [Test]
    public void EnsureNoArgs_WithArgs_Throws()
    {
        var ltext = new LText("k", "f", 1);
        Action act = () => ltext.EnsureNoArgs();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ToString_UsesTextProvider()
    {
        var ltext = new LText("key4", "fallback4");
        var str = ltext.ToString();
        str.Should().Be(TextProvider.Current.GetText(ltext));
    }

    [Test]
    public void ToString_FormatArgs()
    {
        var ltext = new LText("x={0},y={1}").WithArgs(1, "a");
        var str = ltext.ToString();
        str.Should().Be("x=1,y=a");
    }

    [Test]
    public void ImplicitCast_LTextToString()
    {
        var ltext = new LText("key5", "fallback5");
        string? str = ltext;
        str.Should().Be(TextProvider.Current.GetText(ltext));
    }

    [Test]
    public void ImplicitCast_NullableLTextToString()
    {
        LText? ltext = null;
        string? str = ltext;
        str.Should().BeNull();
    }

    [Test]
    public void ImplicitCast_StringToLText()
    {
        LText ltext = "abc";
        ltext.KeyAndOrText.Should().Be("abc");
        ltext.FallbackText.Should().BeNull();
    }

    [Test]
    public void Equals_And_Operators()
    {
        var l1 = new LText("k", "f", 1);
        var l2 = new LText("k", "f", 1);
        var l3 = new LText("k", "f", 2);
        l1.Equals(l2).Should().BeTrue();
        l1.Equals((object)l2).Should().BeTrue();
        (l1 == l2).Should().BeTrue();
        (l1 != l2).Should().BeFalse();
        l1.Equals(l3).Should().BeFalse();
        (l1 == l3).Should().BeFalse();
        (l1 != l3).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_EqualObjects_SameHash()
    {
        var l1 = new LText("k", "f", 1);
        var l2 = new LText("k", "f", 1);
        l1.GetHashCode().Should().Be(l2.GetHashCode());
    }

    [Test]
    public void GetHashCode_DifferentObjects_DifferentHash()
    {
        var l1 = new LText("k", "f", 1);
        var l2 = new LText("k", "f", 2);
        l1.GetHashCode().Should().NotBe(l2.GetHashCode());
    }
}