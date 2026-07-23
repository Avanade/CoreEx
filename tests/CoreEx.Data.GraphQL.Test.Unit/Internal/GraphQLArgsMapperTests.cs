using CoreEx.Data;
using CoreEx.Data.GraphQL.Internal;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLArgsMapperTests
{
    [Test]
    public void BuildQueryArgs_MapsFilterAndOrderBy()
    {
        var args = new Dictionary<string, object?> { ["filter"] = "name eq 'x'", ["orderby"] = "name desc" };
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
    public void BuildPagingArgs_MapsSkipTakeCount()
    {
        var args = new Dictionary<string, object?> { ["skip"] = 10, ["take"] = 25, ["count"] = true };
        var pa = GraphQLArgsMapper.BuildPagingArgs(args);

        pa.Skip.Should().Be(10);
        pa.Take.Should().Be(25);
        pa.IsCountRequested.Should().BeTrue();
    }

    [Test]
    public void BuildPagingArgs_NoArgs_Defaults()
    {
        var pa = GraphQLArgsMapper.BuildPagingArgs(new Dictionary<string, object?>());

        pa.Skip.Should().Be(0);
        pa.IsCountRequested.Should().BeFalse();
    }
}
