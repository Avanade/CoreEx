using CoreEx.Data;
using CoreEx.Data.Querying;
using CoreEx.Http;
using System.Linq.Dynamic.Core;

namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QuerySearchParserTests
{
    private record Person(string FirstName, string? LastName, string Code);

    private static IQueryable<Person> People => new[]
    {
        new Person("Bob", "Brown", "AB-100"),
        new Person("Zoe", "Smith", "AB-200"),
        new Person("Angela", "Smith", "XY-300"),
        new Person("Brownie", null, "ZZ-400")
    }.AsQueryable();

    private static QueryArgsConfig CreateConfig(Action<QuerySearchParser>? configure = null) => QueryArgsConfig.Create()
        .WithFilter(f => f.AddField<string>("LastName"))
        .WithOrderBy(o => o.AddField("FirstName", c => c.WithDefault()))
        .WithSearch(s =>
        {
            s.AddKeywordField("FirstName", c => c.AsUpperCase())
             .AddKeywordField("LastName", c => c.AsUpperCase())
             .AddKeywordField("Code", c => c.WithOperator(QueryFilterOperator.StartsWith).AsUpperCase());

            configure?.Invoke(s);
        });

    [Test]
    public void Parse_NullOrWhiteSpace_NoText()
    {
        var config = CreateConfig();
        config.SearchParser.Parse(null).HasText.Should().BeFalse();
        config.SearchParser.Parse("   ").HasText.Should().BeFalse();
        config.Parse(new QueryArgs { Search = "  " }).SearchResult.Should().BeNull();
    }

    [Test]
    public void Parse_Normalizes_And_Dedupes()
    {
        var result = CreateConfig().SearchParser.Parse("  smith \t Brown  SMITH ").ThrowOnError();
        result.Text.Should().Be("smith Brown SMITH");
        result.Terms.Should().BeEquivalentTo(["smith", "Brown"], o => o.WithStrictOrdering());
        result.IsRanked.Should().BeFalse();
    }

    [Test]
    public void Parse_Keyword_ToLinqString()
    {
        var result = CreateConfig().SearchParser.Parse("smith ab").ThrowOnError();
        result.ToLinqString(out var args).Should().Be(
            "((FirstName != null && FirstName.ToUpper().Contains(@0)) || (LastName != null && LastName.ToUpper().Contains(@1)) || (Code != null && Code.ToUpper().StartsWith(@2)))"
            + " && ((FirstName != null && FirstName.ToUpper().Contains(@3)) || (LastName != null && LastName.ToUpper().Contains(@4)) || (Code != null && Code.ToUpper().StartsWith(@5)))");

        args.Should().BeEquivalentTo(["SMITH", "SMITH", "SMITH", "AB", "AB", "AB"], o => o.WithStrictOrdering());
    }

    [Test]
    public void Parse_Keyword_Field_Options()
    {
        var config = QueryArgsConfig.Create().WithSearch(s => s
            .WithDefaultModelPrefix("P")
            .AddKeywordField("A", c => c.AsLowerCase().WithNoNullCheck().WithOperator(QueryFilterOperator.Equal))
            .AddKeywordField("B", c => c.WithNoModelPrefix().WithOperator(QueryFilterOperator.EndsWith)));

        config.SearchParser.Parse("Xy").ToLinqString(out var args).Should().Be("(P.A.ToLower() == @0 || (B != null && B.EndsWith(@1)))");
        args.Should().BeEquivalentTo(["xy", "Xy"], o => o.WithStrictOrdering());
    }

    [Test]
    public void Parse_MaxLength_Error()
    {
        var result = CreateConfig(s => s.WithMaxLength(5)).SearchParser.Parse("abcdef");
        result.HasError.Should().BeTrue();
        result.Error!.Messages![0].Text.ToString().Should().Be("Statement must not exceed 5 characters in length.");
        result.Error.Messages[0].Property.Should().Be(HttpNames.QuerySearchQueryStringName);
        result.Error.Extensions.Should().ContainKey("schema");
    }

    [Test]
    public void Parse_MaxTerms_Error()
    {
        var result = CreateConfig(s => s.WithMaxTerms(2)).SearchParser.Parse("a b c");
        result.HasError.Should().BeTrue();
        result.Error!.Messages![0].Text.ToString().Should().Be("Statement must not contain more than 2 distinct terms.");

        // Duplicates are not counted.
        CreateConfig(s => s.WithMaxTerms(2)).SearchParser.Parse("a b A").HasError.Should().BeFalse();
    }

    [Test]
    public void Parse_MaxTerms_NotEnforced_WhenRanked()
    {
        var result = QueryArgsConfig.Create().WithSearch(s => s.WithMode(QuerySearchMode.Semantic).WithMaxTerms(1)).SearchParser.Parse("cheap red bike");
        result.HasError.Should().BeFalse();
        result.IsRanked.Should().BeTrue();
        result.ToLinqString(out var args).Should().BeNull();
        args.Should().BeEmpty();
    }

    [Test]
    public void Parse_Validator_Error()
    {
        var result = CreateConfig(s => s.WithValidator(r => { if (r.Terms.Any(t => t.Length < 2)) throw new QuerySearchParserException("Each term must be at least 2 characters."); })).SearchParser.Parse("ab c");
        result.HasError.Should().BeTrue();
        result.Error!.Messages![0].Text.ToString().Should().Be("Each term must be at least 2 characters.");
    }

    [Test]
    public void Parse_Keyword_NoFields_Throws()
    {
        var config = QueryArgsConfig.Create().WithSearch(_ => { });
        FluentActions.Invoking(() => config.SearchParser.Parse("x")).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Config_SearchNotSupported_Error()
    {
        var result = QueryArgsConfig.Create().Parse(new QueryArgs { Search = "x" });
        result.HasError.Should().BeTrue();
        result.Error.Should().BeOfType<QuerySearchParserException>();
        ((ValidationException)result.Error!).Messages![0].Text.ToString().Should().Be("Search statement is not currently supported.");
    }

    [Test]
    public void Config_Keyword_WithOrderBy_Allowed()
    {
        var result = CreateConfig().Parse(new QueryArgs { Search = "smith", OrderBy = "firstname desc" }).ThrowOnError();
        result.SearchResult.Should().NotBeNull();
        result.IsRankedSearch.Should().BeFalse();
        result.OrderByResult!.ToLinqString().Should().Be("FirstName desc");
    }

    [Test]
    public void Config_Ranked_WithOrderBy_Error()
    {
        var result = CreateConfig(s => s.WithMode(QuerySearchMode.Hybrid)).Parse(new QueryArgs { Search = "smith", OrderBy = "firstname" });
        result.HasError.Should().BeTrue();
        result.Error.Should().BeOfType<QueryOrderByParserException>();
    }

    [Test]
    public void Config_Ranked_NoDefaultOrderBy()
    {
        var config = CreateConfig(s => s.WithMode(QuerySearchMode.Semantic));
        var result = config.Parse(new QueryArgs { Search = "smith" }).ThrowOnError();
        result.IsRankedSearch.Should().BeTrue();
        result.SearchResult!.Text.Should().Be("smith");
        result.OrderByResult.Should().BeNull();

        // Without search text the default order-by is applied as per usual.
        config.Parse(new QueryArgs()).OrderByResult!.ToLinqString().Should().Be("FirstName");
    }

    [Test]
    public void Config_ToJsonSchema_IncludesSearch()
    {
        var json = CreateConfig(s => s.WithHelpText("Searches names.")).ToJsonSchema();
        var search = json.GetProperty("search");
        search.GetProperty("mode").GetString().Should().Be("keyword");
        search.GetProperty("ranked").GetBoolean().Should().BeFalse();
        search.GetProperty("maxLength").GetInt32().Should().Be(QuerySearchParser.DefaultMaxLength);
        search.GetProperty("maxTerms").GetInt32().Should().Be(QuerySearchParser.DefaultMaxTerms);
        search.GetProperty("description").GetString().Should().Be("Searches names.");
        search.TryGetProperty("fields", out _).Should().BeFalse();
    }

    [Test]
    public void QueryExtensions_Where_ParseResult_AppliesFilterAndSearch()
    {
        var config = CreateConfig();
        var parsed = config.Parse(new QueryArgs { Filter = "lastname eq 'Smith'", Search = "zo" }).ThrowOnError();
        People.Where(parsed).OrderBy(parsed).Select(p => p.FirstName).ToArray().Should().BeEquivalentTo(["Zoe"]);
    }

    [Test]
    public void QueryExtensions_Where_ParseResult_AllTermsMustMatch()
    {
        var config = CreateConfig();
        var parsed = config.Parse(new QueryArgs { Search = "brown ab" }).ThrowOnError();
        People.Where(parsed).OrderBy(parsed).Select(p => p.FirstName).ToArray().Should().BeEquivalentTo(["Bob"]);

        // Null field values are safely ignored (Brownie has no LastName).
        parsed = config.Parse(new QueryArgs { Search = "brown" }).ThrowOnError();
        People.Where(parsed).OrderBy(parsed).Select(p => p.FirstName).ToArray().Should().BeEquivalentTo(["Bob", "Brownie"], o => o.WithStrictOrdering());
    }

    [Test]
    public void QueryExtensions_Where_ParseResult_Ranked_NotApplied()
    {
        var config = CreateConfig(s => s.WithMode(QuerySearchMode.Semantic));
        var parsed = config.Parse(new QueryArgs { Search = "nothing-matches" }).ThrowOnError();
        People.Where(parsed).Count().Should().Be(4);
    }

    [Test]
    public void QueryExtensions_Where_QueryArgs_AppliesFilterAndSearch()
    {
        var config = CreateConfig();
        People.Where(config, new QueryArgs { Filter = "lastname eq 'Smith'", Search = "xy" }).Select(p => p.FirstName).ToArray().Should().BeEquivalentTo(["Angela"]);
        People.WhereSearch(config, "ab").Count().Should().Be(2);
        FluentActions.Invoking(() => People.WhereSearch(QueryArgsConfig.Create(), "ab")).Should().Throw<QuerySearchParserException>();
        People.WhereSearch(QueryArgsConfig.Create(), null).Count().Should().Be(4);
    }
}
