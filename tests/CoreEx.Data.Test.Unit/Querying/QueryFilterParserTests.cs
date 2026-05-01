using CoreEx.Data.Querying;
using CoreEx.Data.Querying.Expressions;
using CoreEx.Entities;

namespace CoreEx.Data.Test.Unit.Querying;

[TestFixture]
public class QueryFilterParserTests
{
    // NOTHING...
    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("  ", null)]

    // COMPARISON...
    [TestCase("lastname eq 'Smith'", "(LastName != null && LastName == @0)", "Smith")]
    [TestCase("lastname eq null", "LastName == null")]
    [TestCase("firstname eq 'Angela'", "FirstName.ToUpper() == @0", "ANGELA")]
    [TestCase("code eq 'Xyz'", "Code == @0", "Xyz")]
    [TestCase("age lt 100", "Age < @0", 100)]
    [TestCase("age le 100", "Age <= @0", 100)]
    [TestCase("age gt 100", "Age > @0", 100)]
    [TestCase("age ge 100", "Age >= @0", 100)]
    [TestCase("salary gt 1036.42", "Salary > @0", 1036.42)]
    [TestCase("isold eq true", "IsOld == true")]
    [TestCase("IsOld ne false", "IsOld != false")]
    [TestCase("ISOLD ne null", "IsOld != null")]
    [TestCase("isold", "IsOld")]
    [TestCase("messagetype eq 'info'", "MessageType == @0", MessageType.Info)]

    // IN...
    [TestCase("code in ('abc', 'def')", "Code in (@0, @1)", "abc", "def")]
    [TestCase("age in (20, 30, 40)", "Age in (@0, @1, @2)", 20, 30, 40)]
    [TestCase("age in (20)", "Age in (@0)", 20)]

    // AND/OR...
    [TestCase("(age eq 1 or age eq 2) and isold eq true", "(Age == @0 || Age == @1) && IsOld == true", 1, 2)]
    [TestCase("  (  age  eq  1  or  age  eq  2 )  and   isold    ", "(Age == @0 || Age == @1) && IsOld", 1, 2)]
    [TestCase("(age eq 1 or age eq 2) or (age eq 8 or age eq 9)", "(Age == @0 || Age == @1) || (Age == @2 || Age == @3)", 1, 2, 8, 9)]
    [TestCase("((age eq 1 or age eq 2) or (age eq 8 or age eq 9))", "((Age == @0 || Age == @1) || (Age == @2 || Age == @3))", 1, 2, 8, 9)]

    // FUNCTIONS...
    [TestCase("startswith(firstName, 'abc')", "FirstName.ToUpper().StartsWith(@0)", "ABC")]
    [TestCase("endswith(firstName, 'abc')", "FirstName.ToUpper().EndsWith(@0)", "ABC")]
    [TestCase("contains(firstName, 'abc')", "FirstName.ToUpper().Contains(@0)", "ABC")]
    [TestCase("contains(lastname, 'xyz')", "(LastName != null && LastName.Contains(@0))", "xyz")]

    // NOT...
    [TestCase("not (age eq 1)", "!(Age == @0)", 1)]
    [TestCase("age eq 1 and not (age eq 2)", "Age == @0 && !(Age == @1)", 1, 2)]

    // LITERALS...
    [TestCase("code eq ''", "Code == @0", "")]
    [TestCase("code eq ''''", "Code == @0", "'")]
    [TestCase("code eq 'x''x'", "Code == @0", "x'x")]
    [TestCase("code eq 'x'''", "Code == @0", "x'")]
    [TestCase("code eq '''x'", "Code == @0", "'x")]
    [TestCase("code eq '''x'''", "Code == @0", "'x'")]
    [TestCase("code eq 'null'", "Code == @0", "null")]

    // NULL_FIELD...
    [TestCase("terminated eq null", "TerminatedDate == null")]
    [TestCase("terminated ne null", "TerminatedDate != null")]
    public void Parse_Success(string? filter, string? expected, params object[] expectedArgs) => TestUtility.AssertFilterSuccess(filter, expected, expectedArgs);

    [Test]
    public void Parse_Dates_Success()
    {
        TestUtility.AssertFilterSuccess("birthday eq 1980-01-01", "BirthDate == @0", new DateTime(1980, 1, 1));
        TestUtility.AssertFilterSuccess("birthday ne 1980-01-01", "BirthDate != @0", new DateTime(1980, 1, 1));
    }

    // COMPARISON...
    [TestCase("banana", "Field 'banana' is not supported.")]
    [TestCase("banana eq", "Field 'banana' is not supported.")]
    [TestCase("age apple", "Field 'age' does not support 'apple' as an operator.")]
    [TestCase("age 'apple'", "Field 'age' does not support ''apple'' as an operator.")]
    [TestCase("age eq 'apple'", "Field 'age' constant 'apple' must not be specified as a Literal where the underlying type is not a string.")]
    [TestCase("age eq 1990-01-01", "Field 'age' has a value '1990-01-01' that is not a valid Int32: The input string '1990-01-01' was not in a correct format.")]
    [TestCase("null eq null", "There is a 'null' positioning that is syntactically incorrect.")]
    [TestCase("true eq null", "There is a 'true' positioning that is syntactically incorrect.")]
    [TestCase("false eq null", "There is a 'false' positioning that is syntactically incorrect.")]
    [TestCase("and", "There is a 'and' positioning that is syntactically incorrect.")]
    [TestCase("or", "There is a 'or' positioning that is syntactically incorrect.")]
    [TestCase("and age eq 1", "There is a 'and' positioning that is syntactically incorrect.")]
    [TestCase("or age eq 1", "There is a 'or' positioning that is syntactically incorrect.")]
    [TestCase("age eq 1 and", "The final expression is incomplete.")]
    [TestCase("age eq 1 or", "The final expression is incomplete.")]
    [TestCase("isold ge true", "Field 'isold' does not support the 'ge' operator.")]
    [TestCase("age xx 1", "Field 'age' does not support 'xx' as an operator.")]
    [TestCase("age ge null", "Field 'age' constant must not be null for an 'ge' operator.")]
    [TestCase("age eq null", "Field 'age' constant 'null' is not supported.")]
    [TestCase("messagetype eq 'wonky'", "Field 'messagetype' has a value 'wonky' that is not a valid MessageType.")]

    // BRACKETS...
    [TestCase("(age eq 1", "There is an opening '(' that has no matching closing ')'.")]
    [TestCase("age eq 1)", "There is a closing ')' that has no matching opening '('.")]
    [TestCase("age ( 1", "Field 'age' does not support '(' as an operator.")]
    [TestCase("age eq (", "Field 'age' constant '(' is not considered valid.")]
    [TestCase("age eq )", "Field 'age' constant ')' is not considered valid.")]

    // IN...
    [TestCase("code in", "The final expression is incomplete.")]
    [TestCase("code in ()", "Field 'code' constant must be specified before the closing ')' for the 'in' operator.")]
    [TestCase("code in (null)", "Field 'code' constant must not be null for an 'in' operator.")]
    [TestCase("code in ))", "Field 'code' must specify an opening '(' for the 'in' operator.")]
    [TestCase("code in ((", "Field 'code' must close ')' the 'in' operator before specifying a further open '('.")]
    [TestCase("code in (,)", "Field 'code' constant ',' is not considered valid.")]
    [TestCase("age in (1 2)", "Field 'age' expects a ',' separator between constant values for an 'in' operator.")]

    // AND/OR...
    [TestCase("or age eq 1", "There is a 'or' positioning that is syntactically incorrect.")]
    [TestCase("and age eq 1", "There is a 'and' positioning that is syntactically incorrect.")]
    [TestCase("age or eq 1", "Field 'age' does not support 'or' as an operator.")]
    [TestCase("age eq and 1", "Field 'age' constant 'and' is not considered valid.")]
    [TestCase("age eq 1 and and age eq 2", "There is a 'and' positioning that is syntactically incorrect.")]
    [TestCase("age eq 1 or or age eq 2", "There is a 'or' positioning that is syntactically incorrect.")]

    // FUNCTIONS...
    [TestCase("startswith(code, 'abc')", "Field 'code' does not support the 'startswith' function.")]
    [TestCase("startswith)code, 'abc')", "A 'startswith' function expects an opening '(' not a ')'.")]
    [TestCase("startswith(firstname( 'abc')", "A 'startswith' function expects a ',' separator between the field and its constant.")]
    [TestCase("startswith(firstname, null)", "A 'startswith' function references a null constant which is not supported.")]
    [TestCase("startswith(firstname, 'abc',", "A 'startswith' function expects a closing ')' not a ','.")]

    // NOT...
    [TestCase("age eq 1 and not age eq 2", "A 'not' expects an opening '(' to start an expression versus a syntactically incorrect 'age' token.")]
    [TestCase("age  eq  1  not", "There is a 'not' positioning that is syntactically incorrect.")]

    // LITERALS...
    [TestCase("code eq '", "A Literal has not been terminated.")]
    [TestCase("code eq '''", "A Literal has not been terminated.")]
    [TestCase("code eq '''''", "A Literal has not been terminated.")]
    [TestCase("code eq 1", "Field 'code' constant '1' must be specified as a Literal where the underlying type is a string.")]
    [TestCase("age eq '8'", "Field 'age' constant '8' must not be specified as a Literal where the underlying type is not a string.")]

    // DATES...
    [TestCase("birthday eq '32'", "Field 'birthday' constant '32' must not be specified as a Literal where the underlying type is not a string.")]
    [TestCase("birthday eq kiwifruit", "Field 'birthday' constant 'kiwifruit' is not considered valid.")]
    [TestCase("birthday eq 1980-13-01", "Field 'birthday' has a value '1980-13-01' that is not a valid DateTime: String '1980-13-01' was not recognized as a valid DateTime.")]
    [TestCase("birthday eq 1980-01-32", "Field 'birthday' has a value '1980-01-32' that is not a valid DateTime: String '1980-01-32' was not recognized as a valid DateTime.")]

    // NULL_FIELD...
    [TestCase("terminated eq 13", "Field 'terminated' with value '13' is invalid: Only null comparisons are supported.")]
    [TestCase("terminated gt null", "Field 'terminated' does not support the 'gt' operator.")]
    public void Parse_Error(string? filter, string expected) => TestUtility.AssertFilterError(filter, expected);

    [TestCase("lastname eq 'Smith'", "LastName == @0", "Smith")]
    [TestCase(null, "LastName == @0", "Brown")]
    [TestCase("firstname eq 'Jenny'", "FirstName == @0 && LastName == @1", "Jenny", "Brown")]
    public void Parse_WithFieldDefault(string? filter, string expected, params object[] expectedArgs)
    {
        var config = QueryArgsConfig.Create()
            .WithFilter(filter => filter
                .AddField<string>("LastName", c => c.WithDefault(new QueryStatement("LastName == @0", "Brown")))
                .AddField<string>("FirstName")
                .WithDefault(new QueryStatement("FirstName == @0", "Zoe")));

        TestUtility.AssertFilterSuccess(config, filter, expected, expectedArgs);
    }

    [TestCase("lastname eq 'Smith'", "LastName == @0", "Smith")]
    [TestCase("", "FirstName == @0", "Zoe")]
    [TestCase(null, "FirstName == @0", "Zoe")]
    public void Parse_WithDefault(string? filter, string expected, params object[] expectedArgs)
    {
        var config = QueryArgsConfig.Create()
            .WithFilter(filter => filter
                .AddField<string>("LastName")
                .AddField<string>("FirstName")
                .WithDefault(new QueryStatement("FirstName == @0", "Zoe")));

        TestUtility.AssertFilterSuccess(config, filter, expected, expectedArgs);
    }

    [TestCase(true, "lastname eq 'Smith'", "LastName == @0", "Smith")]
    [TestCase(true, "firstname eq 'Angela'", "FirstName == @0 && LastName != null", "Angela")]
    [TestCase(true, null, "LastName != null")]
    [TestCase(false, "lastname eq 'Smith' and firstname eq 'Angela'", "Only a single field filter is allowed.")]
    public void Parse_OnQuery(bool success, string? filter, string expected, params object[] expectedArgs)
    {
        var config = QueryArgsConfig.Create()
            .WithFilter(filter => filter
                .AddField<string>("LastName")
                .AddField<string>("FirstName")
                .OnQuery(result =>
                {
                    if (!result.Fields.Contains("LastName"))
                        result.Writer.AppendStatement(new QueryStatement("LastName != null"));

                    if (result.Fields.Count > 1)
                        throw new QueryFilterParserException("Only a single field filter is allowed.");
                }));

        if (success)
            TestUtility.AssertFilterSuccess(config, filter, expected, expectedArgs);
        else
            TestUtility.AssertFilterError(config, filter, expected);
    }

    [TestCase("lastname ne 'abc'", "LastName != @0", "abc")]
    [TestCase("lastname eq 'abc'", "LastName EQUALS @0", "abc")]
    public void Parse_WithResultWriter(string? filter, string expected, string expectedArgs)
    {
        static bool LastNameWriter(IQueryFilterFieldStatementExpression expression, QueryFilterParserWriter writer)
        {
            if (expression is QueryFilterOperatorExpression oex && oex.Operator.Kind == QueryFilterTokenKind.Equal)
            {
                writer.AppendStatement(new QueryStatement("LastName EQUALS @0", oex.GetConstantValue(0)));
                return true;
            }

            return false;
        }

        var config = QueryArgsConfig.Create()
            .WithFilter(filter => filter
                .AddField<string>("LastName", c => c.WithResultWriter(LastNameWriter))
                .AddField<string>("FirstName"));
    }

    [Test]
    public void Config_ToString()
    {
        var s = TestUtility.Config.FilterParser.ToString();
        s.Should().NotBeNull();

        Console.WriteLine(s);

        s.Should().NotBeNull().And.Be(Resource.GetString("FilterToString.txt"));
    }

    [Test]
    public void Config_ToSchemaDictionary()
    {
        var json = TestUtility.Config.FilterParser.ToJsonSchema();
        json.Should().NotBeNull();

        Console.WriteLine(json.ToString());

        ObjectComparer.AssertJsonFromResource("FilterSchema.json", json.ToString());
    }
}