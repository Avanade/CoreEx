using CoreEx.Events.Subscribing;
using System.Globalization;

namespace CoreEx.Events.Test.Unit.Subscribing;

[TestFixture]
public class SubscribeAttributeTests
{
    [Test]
    public void IsMatch_CaseInsensitivity_IsCultureInvariant()
    {
        // Regression: under tr-TR, 'I' and 'i' are not linguistically case-equivalent (unlike ordinal/invariant), so
        // case-insensitive glob matching must use RegexOptions.CultureInvariant to remain deterministic across cultures.
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var sa = new SubscribeAttribute(title: "coreex.*.invoice.*");
            sa.IsMatch("coreex.system.Invoice.updated", null).Should().BeTrue();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void IsMatch_TitleOnly()
    {
        var sa = new SubscribeAttribute(title: "coreex.*.product.*");
        sa.IsMatch("coreex.system.product.updated", null).Should().BeTrue();
        sa.IsMatch("coreex.system.basket.updated", null).Should().BeFalse();

        // No source configured at all; source-side must not constrain the match.
        sa.IsMatch("coreex.system.product.updated", new Uri("https://any/source")).Should().BeTrue();
    }

    [Test]
    public void IsMatch_SourceOnly_DoesNotThrow()
    {
        // Regression: constructing a source-only SubscribeAttribute and calling IsMatch must not throw
        // (previously threw because the title side unconditionally required a non-null/non-empty title pattern).
        var sa = new SubscribeAttribute(source: "system/product");

        Action act = () => sa.IsMatch(null, new Uri("system/product", UriKind.Relative));
        act.Should().NotThrow();

        sa.IsMatch(null, new Uri("system/product", UriKind.Relative)).Should().BeTrue();
        sa.IsMatch("any.title.at.all", new Uri("system/product", UriKind.Relative)).Should().BeTrue();
        sa.IsMatch("any.title.at.all", new Uri("system/basket", UriKind.Relative)).Should().BeFalse();
    }

    [Test]
    public void IsMatch_Neither_MatchesAnything()
    {
        var sa = new SubscribeAttribute();
        sa.IsMatch(null, null).Should().BeTrue();
        sa.IsMatch("any.title", new Uri("https://any/source")).Should().BeTrue();
    }

    [Test]
    public void IsMatch_TitleAndSource_BothMustMatch()
    {
        var sa = new SubscribeAttribute(title: "coreex.*.product.*", source: "system/product");
        sa.IsMatch("coreex.system.product.updated", new Uri("system/product", UriKind.Relative)).Should().BeTrue();
        sa.IsMatch("coreex.system.product.updated", new Uri("system/basket", UriKind.Relative)).Should().BeFalse();
        sa.IsMatch("coreex.system.basket.updated", new Uri("system/product", UriKind.Relative)).Should().BeFalse();
    }
}
