using CoreEx.Data;
using CoreEx.Data.Querying;
using System.Linq.Dynamic.Core;

namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QueryArgsConfigTests
{
    private record Person(string FirstName, string LastName, int Age);

    private static IQueryable<Person> People => new[]
    {
        new Person("Bob", "Brown", 40),
        new Person("Zoe", "Smith", 30),
        new Person("Angela", "Smith", 25)
    }.AsQueryable();

    private static QueryArgsConfig CreateConfig() => QueryArgsConfig.Create()
        .WithFilter(f => f.AddField<string>("LastName"))
        .WithOrderBy(o => o.AddField("FirstName"));

    [Test]
    public void Parse_Null_ReturnsEmptyResult()
    {
        var result = TestUtility.Config.Parse(null);
        result.HasError.Should().BeFalse();
        result.FilterResult.Should().BeNull();
        result.OrderByResult.Should().BeNull();
    }

    [Test]
    public void Parse_Success_CombinesFilterAndOrderBy()
    {
        var result = TestUtility.Config.Parse(QueryArgs.Create("lastname eq 'Smith'", "firstname"));
        result.HasError.Should().BeFalse();
        result.FilterResult!.ToLinqString(out var args).Should().Be("(LastName != null && LastName == @0)");
        args.Should().BeEquivalentTo(["Smith"]);
        result.OrderByResult!.ToLinqString().Should().Be("FirstName");
    }

    [Test]
    public void Parse_FilterNotSupported_Error()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(o => o.AddField("FirstName"));
        var result = config.Parse(QueryArgs.Create(filter: "lastname eq 'Smith'"));
        result.HasError.Should().BeTrue();
        ((ValidationException)result.Error!).Messages![0].Text.ToString().Should().Be("Filter statement is not currently supported.");
    }

    [Test]
    public void Parse_OrderByNotSupported_Error()
    {
        var config = QueryArgsConfig.Create().WithFilter(f => f.AddField<string>("LastName"));
        var result = config.Parse(QueryArgs.Create(orderBy: "firstname"));
        result.HasError.Should().BeTrue();
        ((ValidationException)result.Error!).Messages![0].Text.ToString().Should().Be("OrderBy statement is not currently supported.");
    }

    [Test]
    public void QueryExtensions_WhereOrderBy_WithStrings_AppliesToQueryable()
    {
        var config = CreateConfig();
        var result = People.Where(config, "lastname eq 'Smith'").OrderBy(config, "firstname").ToArray();

        result.Select(p => p.FirstName).Should().BeEquivalentTo(["Angela", "Zoe"], o => o.WithStrictOrdering());
    }

    [Test]
    public void QueryExtensions_WhereOrderBy_WithQueryArgs_AppliesToQueryable()
    {
        var config = CreateConfig();
        var queryArgs = QueryArgs.Create("lastname eq 'Smith'", "firstname");
        var result = People.Where(config, queryArgs).OrderBy(config, queryArgs).ToArray();

        result.Select(p => p.FirstName).Should().BeEquivalentTo(["Angela", "Zoe"], o => o.WithStrictOrdering());
    }

    [Test]
    public void QueryExtensions_WhereOrderBy_WithParseResult_AppliesToQueryable()
    {
        var config = CreateConfig();
        var parseResult = config.Parse(QueryArgs.Create("lastname eq 'Smith'", "firstname"));
        parseResult.HasError.Should().BeFalse();

        var result = People.Where(parseResult).OrderBy(parseResult).ToArray();

        result.Select(p => p.FirstName).Should().BeEquivalentTo(["Angela", "Zoe"], o => o.WithStrictOrdering());
    }

    [Test]
    public void QueryExtensions_Where_NoFilterParser_ThrowsWhenFilterSpecified()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(o => o.AddField("FirstName"));
        var act = () => People.Where(config, "lastname eq 'Smith'").ToArray();

        act.Should().Throw<QueryFilterParserException>().Which.Messages![0].Text.ToString().Should().Be("Query filter statement is not currently supported.");
    }
}
