using CoreEx.Data;
using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class EntitiesExtensionsTests
{
    private class Item
    {
        public string? Name { get; set; }
    }

    private static List<int> Numbers => [1, 2, 3, 4, 5];

    [Test]
    public void WhereWhen_True_AppliesPredicate()
        => Numbers.WhereWhen(true, i => i > 2).Should().BeEquivalentTo([3, 4, 5]);

    [Test]
    public void WhereWhen_False_ReturnsSourceUnfiltered()
        => Numbers.WhereWhen(false, i => i > 2).Should().BeEquivalentTo(Numbers);

    [Test]
    public void WhereWith_DefaultValue_ReturnsSourceUnfiltered()
        => Numbers.WhereWith(0, i => i > 2).Should().BeEquivalentTo(Numbers);

    [Test]
    public void WhereWith_NonDefaultValue_AppliesPredicate()
        => Numbers.WhereWith(5, i => i > 2).Should().BeEquivalentTo([3, 4, 5]);

    [Test]
    public void WhereWith_NonDefaultString_AppliesPredicate()
        => Numbers.WhereWith("abc", i => i > 2).Should().BeEquivalentTo([3, 4, 5]);

    [Test]
    public void WhereWith_NullWith_ReturnsSourceUnfiltered()
        => Numbers.WhereWith((string?)null, i => i > 2).Should().BeEquivalentTo(Numbers);

    [Test]
    public void WhereWith_EmptyEnumerableWith_ReturnsSourceUnfiltered()
        => Numbers.WhereWith(Array.Empty<int>(), i => i > 2).Should().BeEquivalentTo(Numbers);

    [Test]
    public void WhereWith_NonEmptyEnumerableWith_AppliesPredicate()
        => Numbers.WhereWith(new[] { 1 }, i => i > 2).Should().BeEquivalentTo([3, 4, 5]);

    [Test]
    public void WhereWildcard_Contains_FiltersMatchingItems()
    {
        var items = new[] { new Item { Name = "Bob Smith" }, new Item { Name = "Alice Jones" } };
        var result = items.WhereWildcard(x => x.Name, "*Smith*");
        result.Should().ContainSingle().Which.Name.Should().Be("Bob Smith");
    }

    [Test]
    public void WhereWildcard_NullSelectorValue_CheckForNullTrue_ExcludesNull()
    {
        var items = new[] { new Item { Name = null }, new Item { Name = "Bob Smith" } };
        var result = items.WhereWildcard(x => x.Name, "*Smith*", checkForNull: true);
        result.Should().ContainSingle().Which.Name.Should().Be("Bob Smith");
    }

    [Test]
    public void WhereWildcard_NullPattern_ReturnsAllItems()
    {
        var items = new[] { new Item { Name = "Bob" }, new Item { Name = "Alice" } };
        var result = items.WhereWildcard(x => x.Name, null);
        result.Should().BeEquivalentTo(items);
    }

    [Test]
    public void WhereWildcard_NullSelector_Throws()
    {
        var items = new[] { new Item { Name = "Bob" } };
        Action act = () => items.WhereWildcard<Item>(null!, "*Bob*").ToList();
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void WithPaging_NullPaging_UsesDefault()
    {
        var result = Numbers.WithPaging().ToList();
        result.Should().BeEquivalentTo(Numbers);
    }

    [Test]
    public void WithPaging_SkipAndTake_ReturnsSubset()
    {
        var result = Numbers.WithPaging(PagingArgs.Create(skip: 1, take: 2)).ToList();
        result.Should().BeEquivalentTo([2, 3], o => o.WithStrictOrdering());
    }

    [Test]
    public void WithPaging_None_ReturnsSourceUnfiltered()
    {
        var result = Numbers.WithPaging(PagingArgs.None).ToList();
        result.Should().BeEquivalentTo(Numbers);
    }

    [TestCase(FeatureSupport.NotSupported, true, false, false, false)]
    [TestCase(FeatureSupport.ReadOnly, false, true, false, true)]
    [TestCase(FeatureSupport.Mutable, false, false, true, true)]
    public void FeatureSupport_Flags_AreCorrect(FeatureSupport support, bool isNone, bool isReadOnly, bool isMutable, bool isSupported)
    {
        support.IsNone.Should().Be(isNone);
        support.IsReadOnly.Should().Be(isReadOnly);
        support.IsMutable.Should().Be(isMutable);
        support.IsSupported.Should().Be(isSupported);
    }

    private interface IMutableFeature { }
    private interface IReadOnlyFeature { }
    private class MutableThing : IMutableFeature, IReadOnlyFeature { }
    private class ReadOnlyThing : IReadOnlyFeature { }
    private class UnsupportedThing { }

    [Test]
    public void Determine_ImplementsMutable_ReturnsMutable()
        => EntitiesExtensions.Determine<MutableThing, IMutableFeature, IReadOnlyFeature>().Should().Be(FeatureSupport.Mutable);

    [Test]
    public void Determine_ImplementsReadOnlyOnly_ReturnsReadOnly()
        => EntitiesExtensions.Determine<ReadOnlyThing, IMutableFeature, IReadOnlyFeature>().Should().Be(FeatureSupport.ReadOnly);

    [Test]
    public void Determine_ImplementsNeither_ReturnsNotSupported()
        => EntitiesExtensions.Determine<UnsupportedThing, IMutableFeature, IReadOnlyFeature>().Should().Be(FeatureSupport.NotSupported);
}
