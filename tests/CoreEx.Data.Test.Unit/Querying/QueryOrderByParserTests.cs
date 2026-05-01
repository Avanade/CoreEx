namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QueryOrderByParserTests
{
    [TestCase("firstname asc, birthday", "FirstName, BirthDate")]
    [TestCase("lastname asc, birthday desc", "LastName, BirthDate desc")]
    [TestCase(null, "LastName, FirstName")]
    public void Parse_Success(string? filter, string expected) => TestUtility.AssertOrderBySuccess(filter, expected);

    [TestCase("firstname, middlename", "Field 'middlename' is not supported.")]
    [TestCase("firstname, birthday asc", "Field 'birthday' direction 'asc' is invalid; not supported.")]
    [TestCase("firstname, birthday both", "Field 'birthday' direction 'both' is invalid; must be either 'asc' (ascending) or 'desc' (descending).")]
    [TestCase("firstname asc, firstname desc", "Field 'firstname' must not be specified more than once.")]
    [TestCase("firstname asc desc", "Statement is syntactically incorrect.")]
    public void Parse_Error(string? filter, string expected) => TestUtility.AssertOrderByError(filter, expected);

    [Test]
    public void Config_ToString()
    {
        var s = TestUtility.Config.OrderByParser.ToString();
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