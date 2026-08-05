using CoreEx.Data;

namespace CoreEx.Test.Unit.Data;

[TestFixture]
public class DataExtensionsTests
{
    private class Item
    {
        public string? Name { get; set; }
    }

    private static IQueryable<int> Numbers => new List<int> { 1, 2, 3, 4, 5 }.AsQueryable();

    private static IQueryable<Item> Items => new List<Item>
    {
        new() { Name = "Bob Smith" },
        new() { Name = "Alice Jones" },
        new() { Name = null }
    }.AsQueryable();

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
    public void WhereWith_EmptyEnumerableWith_ReturnsSourceUnfiltered()
        => Numbers.WhereWith(Array.Empty<int>(), i => i > 2).Should().BeEquivalentTo(Numbers);

    [Test]
    public void WhereWith_NonEmptyEnumerableWith_AppliesPredicate()
        => Numbers.WhereWith(new[] { 1 }, i => i > 2).Should().BeEquivalentTo([3, 4, 5]);

    [Test]
    public void WhereWildcard_Contains_FiltersMatchingItems()
        => Items.WhereWildcard(x => x.Name, "*Smith*").Should().ContainSingle().Which.Name.Should().Be("Bob Smith");

    [Test]
    public void WhereWildcard_StartsWith_FiltersMatchingItems()
        => Items.WhereWildcard(x => x.Name, "Bob*").Should().ContainSingle().Which.Name.Should().Be("Bob Smith");

    [Test]
    public void WhereWildcard_EndsWith_FiltersMatchingItems()
        => Items.WhereWildcard(x => x.Name, "*Jones").Should().ContainSingle().Which.Name.Should().Be("Alice Jones");

    [Test]
    public void WhereWildcard_Equal_FiltersExactMatch()
        => Items.WhereWildcard(x => x.Name, "Bob Smith").Should().ContainSingle().Which.Name.Should().Be("Bob Smith");

    [Test]
    public void WhereWildcard_IgnoreCase_MatchesRegardlessOfCase()
        => Items.WhereWildcard(x => x.Name, "*smith*", ignoreCase: true).Should().ContainSingle().Which.Name.Should().Be("Bob Smith");

    [Test]
    public void WhereWildcard_NullPattern_ReturnsAllItems()
        => Items.WhereWildcard(x => x.Name, null).Should().BeEquivalentTo(Items);

    [Test]
    public void WhereWildcard_NullSelector_Throws()
    {
        Action act = () => Items.WhereWildcard<Item>(null!, "*Bob*").ToList();
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void WhereWildcard_NonMemberExpressionSelector_Throws()
    {
        Action act = () => Items.WhereWildcard(x => x.Name!.ToUpper(), "*BOB*").ToList();
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WithPaging_NullPaging_UsesDefault()
        => Numbers.WithPaging().ToList().Should().BeEquivalentTo(Numbers);

    [Test]
    public void WithPaging_SkipAndTake_ReturnsSubset()
        => Numbers.WithPaging(PagingArgs.Create(skip: 1, take: 2)).ToList().Should().BeEquivalentTo([2, 3], o => o.WithStrictOrdering());

    [Test]
    public void WithPaging_None_ReturnsSourceUnfiltered()
        => Numbers.WithPaging(PagingArgs.None).ToList().Should().BeEquivalentTo(Numbers);

    [Test]
    public void WithTotalCount_NotRequested_DoesNotSetTotalCount()
    {
        // Invoked via explicit static syntax to bypass PagingResult's own like-named instance method and exercise the DataExtensions.WithTotalCount(long) guard directly.
        var result = DataExtensions.WithTotalCount(new PagingResult(PagingArgs.Create()), 100L);
        result.TotalCount.Should().BeNull();
    }

    [Test]
    public void WithTotalCount_Requested_SetsTotalCount()
    {
        var result = DataExtensions.WithTotalCount(new PagingResult(PagingArgs.CreateWithCount()), 100L);
        result.TotalCount.Should().Be(100);
    }

    [Test]
    public void WithTotalCount_Func_Requested_SetsTotalCount()
    {
        var result = new PagingResult(PagingArgs.CreateWithCount()).WithTotalCount(() => 42);
        result.TotalCount.Should().Be(42);
    }

    [Test]
    public void WithTotalCount_Func_Requested_ExceptionSwallowed_LeavesTotalCountNull()
    {
        long ThrowingFunc() => throw new InvalidOperationException("boom");

        var result = new PagingResult(PagingArgs.CreateWithCount());
        Action act = () => result.WithTotalCount(ThrowingFunc);

        act.Should().NotThrow();
        result.TotalCount.Should().BeNull();
    }

    [Test]
    public async Task WithTotalCountAsync_Requested_SetsTotalCount()
    {
        var result = await new PagingResult(PagingArgs.CreateWithCount()).WithTotalCountAsync(() => Task.FromResult<long?>(77));
        result.TotalCount.Should().Be(77);
    }

    [Test]
    public async Task WithTotalCountAsync_NotRequested_DoesNotInvokeFuncOrSetTotalCount()
    {
        var invoked = false;
        var result = await new PagingResult(PagingArgs.Create()).WithTotalCountAsync(() =>
        {
            invoked = true;
            return Task.FromResult<long?>(77);
        });

        invoked.Should().BeFalse();
        result.TotalCount.Should().BeNull();
    }

    [Test]
    public async Task WithTotalCountAsync_Requested_ExceptionSwallowed_LeavesTotalCountNull()
    {
        var result = await new PagingResult(PagingArgs.CreateWithCount()).WithTotalCountAsync(() => throw new InvalidOperationException("boom"));
        result.TotalCount.Should().BeNull();
    }

    [Test]
    public async Task WithTotalCountAsync_WithCancellationToken_PassesTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken? received = null;

        var result = await new PagingResult(PagingArgs.CreateWithCount()).WithTotalCountAsync(ct =>
        {
            received = ct;
            return Task.FromResult<long?>(9);
        }, cts.Token);

        received.Should().Be(cts.Token);
        result.TotalCount.Should().Be(9);
    }
}
