using CoreEx.Data.Querying;

namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QueryOrderByParserTests
{
    [TestCase("lastname asc, birthday desc", "LastName, BirthDate desc")]
    [TestCase(null, "LastName, FirstName")]
    public void Parse_Success(string? filter, string expected) => TestUtility.AssertOrderBySuccess(filter, expected);

    [TestCase("firstname, middlename", "Field 'middlename' is not supported.")]
    [TestCase("firstname, birthday asc", "Field 'birthday' direction 'asc' is invalid; not supported.")]
    [TestCase("firstname, birthday both", "Field 'birthday' direction 'both' is invalid; must be either 'asc' (ascending) or 'desc' (descending).")]
    [TestCase("firstname asc, firstname desc", "Field 'firstname' must not be specified more than once.")]
    [TestCase("firstname asc desc", "Statement is syntactically incorrect.")]
    // Regression: omitting the direction used to bypass a field's WithDirection allow-list entirely (Birthday/BirthDate is configured Descending-only) -
    // this case used to be a Parse_Success ("firstname asc, birthday" -> "FirstName, BirthDate") until the implicit-ascending default was also enforced.
    [TestCase("firstname asc, birthday", "Field 'birthday' direction 'asc' is invalid; not supported.")]
    public void Parse_Error(string? filter, string expected) => TestUtility.AssertOrderByError(filter, expected);

    [Test]
    public void Parse_DefaultOrderBy_Descending_Success()
    {
        // Regression: DefaultOrderBy omitted the space before "desc" for a descending default (e.g. "createddatedesc" instead of "createddate desc"),
        // which broke Parse(null) entirely for any field configured .WithDefault(QueryOrderByDirection.Descending) - it always threw a
        // "Field '...desc' is not supported" error instead of applying the intended default sort.
        var config = QueryArgsConfig.Create().WithOrderBy(order => order.AddField("CreatedDate", c => c.WithDefault(QueryOrderByDirection.Descending)));

        var result = config.OrderByParser.Parse(null);
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("CreatedDate desc");
    }

    [Test]
    public void Parse_AlwaysInclude_AppendedWhenNotPresent()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .AddField("LastName")
            .AddField("Id", c => c.WithAlwaysInclude(QueryOrderByDirection.Descending)));

        var result = config.OrderByParser.Parse("lastname");
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("LastName, Id desc");
    }

    [Test]
    public void Parse_AlwaysInclude_SkippedWhenAlreadyPresent()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .AddField("LastName")
            .AddField("Id", c => c.WithAlwaysInclude()));

        var result = config.OrderByParser.Parse("id desc");
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("Id desc");
    }

    [Test]
    public void Parse_WithValidator_Invoked()
    {
        string[]? seen = null;
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .AddField("LastName")
            .AddField("FirstName")
            .WithValidator(fields => seen = fields));

        var result = config.OrderByParser.Parse("lastname, firstname desc");
        result.HasError.Should().BeFalse();
        seen.Should().BeEquivalentTo(["LastName", "FirstName"], o => o.WithStrictOrdering());
    }

    [Test]
    public void Parse_WithValidator_Throws_Error()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .AddField("LastName")
            .AddField("FirstName")
            .WithValidator(fields =>
            {
                if (fields.Length > 1)
                    throw new QueryOrderByParserException("Only a single order-by field is allowed.");
            }));

        var result = config.OrderByParser.Parse("lastname, firstname");
        result.HasError.Should().BeTrue();
        result.Error!.Messages![0].Text.ToString().Should().Be("Only a single order-by field is allowed.");
    }

    [Test]
    public void Parse_WithDefaultModelPrefix_AppliesToFields()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .WithDefaultModelPrefix("p")
            .AddField("LastName"));

        var result = config.OrderByParser.Parse("lastname");
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("p.LastName");
    }

    [Test]
    public void Parse_WithModelPrefix_OverridesDefault()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .WithDefaultModelPrefix("p")
            .AddField("LastName", c => c.WithModelPrefix("q")));

        var result = config.OrderByParser.Parse("lastname");
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("q.LastName");
    }

    [Test]
    public void Parse_WithNoModelPrefix_ClearsDefault()
    {
        var config = QueryArgsConfig.Create().WithOrderBy(order => order
            .WithDefaultModelPrefix("p")
            .AddField("LastName", c => c.WithNoModelPrefix()));

        var result = config.OrderByParser.Parse("lastname");
        result.HasError.Should().BeFalse();
        result.ToLinqString().Should().Be("LastName");
    }

    [Test]
    public void Config_ToString()
    {
        var s = TestUtility.Config.OrderByParser.ToString().ReplaceLineEndings("\n");
        s.Should().NotBeNull();

        Console.WriteLine(s);

        s.Should().NotBeNull().And.Be(Resource.GetString("OrderByToString.txt"));
    }

    [Test]
    public void Config_ToSchemaDictionary()
    {
        var json = TestUtility.Config.OrderByParser.ToJsonSchema();
        json.Should().NotBeNull();

        Console.WriteLine(json.ToString());

        ObjectComparer.AssertJsonFromResource("OrderBySchema.json", json.ToString());
    }
}
