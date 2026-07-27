using CoreEx.Data;
using CoreEx.Data.GraphQL.Internal;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLArgsMapperTests
{
    [Test]
    public void BuildQueryArgs_MapsWhereAndOrderBy()
    {
        var args = new Dictionary<string, object?>
        {
            ["where"] = new Dictionary<string, object?> { ["name"] = "x" },
            ["orderBy"] = new List<object?> { new Dictionary<string, object?> { ["name"] = "DESC" } }
        };

        var qa = GraphQLArgsMapper.BuildQueryArgs(args);

        qa.Filter.Should().Be("name eq 'x'");
        qa.OrderBy.Should().Be("name desc");
        qa.IsIncludeText.Should().BeFalse();
        qa.IsIncludeInactive.Should().BeFalse();
    }

    [Test]
    public void BuildQueryArgs_MapsIncludeTextAndInactive()
    {
        var args = new Dictionary<string, object?> { ["includeText"] = true, ["includeInactive"] = true };
        var qa = GraphQLArgsMapper.BuildQueryArgs(args);

        qa.IsIncludeText.Should().BeTrue();
        qa.IsIncludeInactive.Should().BeTrue();
    }

    [Test]
    public void BuildQueryArgs_NoArgs_DefaultsEmpty()
    {
        var qa = GraphQLArgsMapper.BuildQueryArgs(new Dictionary<string, object?>());

        qa.Filter.Should().BeNull();
        qa.OrderBy.Should().BeNull();
    }

    [Test]
    public void BuildConnectionPagingArgs_MapsFirstAndCount()
    {
        var args = new Dictionary<string, object?> { ["first"] = 25 };
        var (pa, first, requiresTotalCount) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: true);

        first.Should().Be(25);
        pa.Skip.Should().Be(0);
        pa.Take.Should().Be(26); // Over-fetch by one to derive hasNextPage.
        pa.IsCountRequested.Should().BeTrue();
        requiresTotalCount.Should().BeFalse();
    }

    [Test]
    public void BuildConnectionPagingArgs_DecodesAfterCursorIntoSkip()
    {
        var args = new Dictionary<string, object?> { ["first"] = 10, ["after"] = GraphQLCursor.Encode(4) };
        var (pa, first, _) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        first.Should().Be(10);
        pa.Skip.Should().Be(5);
    }

    [Test]
    public void BuildConnectionPagingArgs_NoArgs_DefaultsFirstAndSkip()
    {
        var (pa, first, _) = GraphQLArgsMapper.BuildConnectionPagingArgs(new Dictionary<string, object?>(), isCountRequested: false);

        first.Should().Be(PagingArgs.DefaultTake);
        pa.Skip.Should().Be(0);
        pa.IsCountRequested.Should().BeFalse();
    }

    [Test]
    public void BuildConnectionPagingArgs_Last_ThrowsTranslationException()
    {
        var args = new Dictionary<string, object?> { ["last"] = 5 };
        var act = () => GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*Backward pagination*");
    }

    [Test]
    public void BuildConnectionPagingArgs_Before_ThrowsTranslationException()
    {
        var args = new Dictionary<string, object?> { ["before"] = "abc" };
        var act = () => GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*Backward pagination*");
    }

    [Test]
    public void BuildConnectionPagingArgs_FirstNotPositive_Throws()
    {
        var args = new Dictionary<string, object?> { ["first"] = 0 };
        var act = () => GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*greater than zero*");
    }

    [Test]
    public void BuildConnectionPagingArgs_InvalidAfterCursor_Throws()
    {
        var args = new Dictionary<string, object?> { ["after"] = "not-a-valid-cursor" };
        var act = () => GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*not a valid cursor*");
    }

    [Test]
    public void BuildConnectionPagingArgs_AfterCursorDecodesToIntMaxValue_ThrowsRatherThanOverflowingAsync()
    {
        // GraphQLCursor.TryDecode allows offset == int.MaxValue (it only validates offset >= 0); without this guard, 'skip = offset + 1' would silently overflow to
        // int.MinValue, which PagingArgs.Skip's setter then clamps to 0 - a crafted 'after' cursor would silently return page 1 instead of erroring.
        var args = new Dictionary<string, object?> { ["after"] = GraphQLCursor.Encode(int.MaxValue) };
        var act = () => GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*out of range*");
    }

    [Test]
    public void BuildConnectionPagingArgs_NeedsItemsFalse_CapsTakeAtOne()
    {
        // A totalCount-only selection (neither edges nor pageInfo requested) should not over-fetch a full page of rows just to discard them.
        var args = new Dictionary<string, object?> { ["first"] = 25 };
        var (pa, first, requiresTotalCount) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: true, needsItems: false);

        first.Should().Be(25);
        pa.Take.Should().Be(1);
        pa.IsCountRequested.Should().BeTrue();
        requiresTotalCount.Should().BeFalse();
    }

    [Test]
    public void BuildConnectionPagingArgs_NeedsItemsTrue_OverFetchesByOne()
    {
        var args = new Dictionary<string, object?> { ["first"] = 25 };
        var (pa, _, _) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false, needsItems: true);

        pa.Take.Should().Be(26);
    }

    [Test]
    public void BuildConnectionPagingArgs_MaximumTakeOne_OverFetchImpossible_RequiresTotalCount()
    {
        // Where PagingArgs.MaximumTake is so small (<= 1) that the usual 'first + 1' over-fetch would itself be clamped straight back down, an over-fetch-based
        // hasNextPage can never be derived; the total count must be forced instead so the caller can derive hasNextPage from PagingResult.TotalCount.
        var originalMaximumTake = PagingArgs.MaximumTake;
        try
        {
            PagingArgs.MaximumTake = 1;
            var args = new Dictionary<string, object?> { ["first"] = 25 };
            var (pa, first, requiresTotalCount) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false, needsItems: true);

            first.Should().Be(1);
            pa.Take.Should().Be(1);
            pa.IsCountRequested.Should().BeTrue("the count must be forced when an over-fetch is structurally impossible");
            requiresTotalCount.Should().BeTrue();
        }
        finally
        {
            PagingArgs.MaximumTake = originalMaximumTake;
        }
    }

    [Test]
    public void ApplyItemRootFlags_NoArgs_DoesNotThrow()
    {
        var act = () => GraphQLArgsMapper.ApplyItemRootFlags(new Dictionary<string, object?>());

        act.Should().NotThrow();
    }

    [Test]
    public void ApplyItemRootFlags_WhereArgPresent_Throws()
    {
        var args = new Dictionary<string, object?> { ["where"] = new Dictionary<string, object?> { ["name"] = "x" } };
        var act = () => GraphQLArgsMapper.ApplyItemRootFlags(args);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*where*orderBy*");
    }

    [Test]
    public void ApplyItemRootFlags_OrderByArgPresent_Throws()
    {
        var args = new Dictionary<string, object?> { ["orderBy"] = new List<object?> { new Dictionary<string, object?> { ["name"] = "DESC" } } };
        var act = () => GraphQLArgsMapper.ApplyItemRootFlags(args);

        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*where*orderBy*");
    }
}
