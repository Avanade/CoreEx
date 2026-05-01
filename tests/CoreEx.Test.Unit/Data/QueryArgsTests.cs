using CoreEx.Data;

namespace CoreEx.Test.Unit.Data;

[TestFixture]
public class QueryArgsTests
{
    [Test]
    public void Create_Default()
    {
        var args = QueryArgs.Create();
        args.Filter.Should().BeNull();
        args.OrderBy.Should().BeNull();
        args.IncludeFields.Should().BeNull();
        args.ExcludeFields.Should().BeNull();
    }

    [Test]
    public void Create_WithValues()
    {
        var args = QueryArgs.Create("x eq 1", "y desc");
        args.Filter.Should().Be("x eq 1");
        args.OrderBy.Should().Be("y desc");
    }

    [Test]
    public void Filter_SetAndGet()
    {
        var args = new QueryArgs() { Filter = "abc" };
        args.Filter.Should().Be("abc");
    }

    [Test]
    public void OrderBy_SetAndGet()
    {
        var args = new QueryArgs
        {
            OrderBy = "def"
        };
        args.OrderBy.Should().Be("def");
    }

    [Test]
    public void IncludeFields_SetAndGet()
    {
        var args = new QueryArgs
        {
            IncludeFields = ["a", "b"]
        };
        args.IncludeFields.Should().BeEquivalentTo("a", "b");
    }

    [Test]
    public void ExcludeFields_SetAndGet()
    {
        var args = new QueryArgs
        {
            ExcludeFields = ["x", "y"]
        };
        args.ExcludeFields.Should().BeEquivalentTo("x", "y");
    }

    [Test]
    public void Include_AddsFields()
    {
        var args = new QueryArgs();
        var ret = args.WithFields("a", "b", "c");
        ret.Should().BeSameAs(args);
        args.IncludeFields.Should().BeEquivalentTo("a", "b", "c");
    }

    [Test]
    public void Include_AddsFields_MultipleCalls()
    {
        var args = new QueryArgs();
        args.WithFields("a").WithFields("b", "c");
        args.IncludeFields.Should().BeEquivalentTo("a", "b", "c");
    }

    [Test]
    public void Exclude_AddsFields()
    {
        var args = new QueryArgs();
        var ret = args.WithoutFields("x", "y");
        ret.Should().BeSameAs(args);
        args.ExcludeFields.Should().BeEquivalentTo("x", "y");
    }

    [Test]
    public void Exclude_AddsFields_MultipleCalls()
    {
        var args = new QueryArgs();
        args.WithoutFields("x").WithoutFields("y", "z");
        args.ExcludeFields.Should().BeEquivalentTo("x", "y", "z");
    }

    [Test]
    public void Include_And_Exclude_AreIndependent()
    {
        var args = new QueryArgs();
        args.WithFields("a", "b");
        args.WithoutFields("x", "y");
        args.IncludeFields.Should().BeEquivalentTo("a", "b");
        args.ExcludeFields.Should().BeEquivalentTo("x", "y");
    }
}