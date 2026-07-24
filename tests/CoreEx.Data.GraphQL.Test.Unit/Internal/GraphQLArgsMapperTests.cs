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
        var (pa, first) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: true);

        first.Should().Be(25);
        pa.Skip.Should().Be(0);
        pa.Take.Should().Be(26); // Over-fetch by one to derive hasNextPage.
        pa.IsCountRequested.Should().BeTrue();
    }

    [Test]
    public void BuildConnectionPagingArgs_DecodesAfterCursorIntoSkip()
    {
        var args = new Dictionary<string, object?> { ["first"] = 10, ["after"] = GraphQLCursor.Encode(4) };
        var (pa, first) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false);

        first.Should().Be(10);
        pa.Skip.Should().Be(5);
    }

    [Test]
    public void BuildConnectionPagingArgs_NoArgs_DefaultsFirstAndSkip()
    {
        var (pa, first) = GraphQLArgsMapper.BuildConnectionPagingArgs(new Dictionary<string, object?>(), isCountRequested: false);

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
    public void BuildConnectionPagingArgs_NeedsItemsFalse_CapsTakeAtOne()
    {
        // A totalCount-only selection (neither edges nor pageInfo requested) should not over-fetch a full page of rows just to discard them.
        var args = new Dictionary<string, object?> { ["first"] = 25 };
        var (pa, first) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: true, needsItems: false);

        first.Should().Be(25);
        pa.Take.Should().Be(1);
        pa.IsCountRequested.Should().BeTrue();
    }

    [Test]
    public void BuildConnectionPagingArgs_NeedsItemsTrue_OverFetchesByOne()
    {
        var args = new Dictionary<string, object?> { ["first"] = 25 };
        var (pa, _) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, isCountRequested: false, needsItems: true);

        pa.Take.Should().Be(26);
    }
}
