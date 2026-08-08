using CoreEx.Data.GraphQL.Internal;
using GraphQLParser;
using GraphQLParser.AST;
using System.Globalization;
using System.Text.Json;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLValueConverterTests
{
    private static GraphQLArguments? ParseArguments(string document)
    {
        var parsed = Parser.Parse(document);
        var operation = (GraphQLOperationDefinition)parsed.Definitions[0];
        var field = (GraphQLField)operation.SelectionSet.Selections[0];
        return field.Arguments;
    }

    [Test]
    public void ConvertArguments_ScalarLiterals()
    {
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(name: \"bob\", age: 42, active: true) { id } }"), null);

        args.GetString("name").Should().Be("bob");
        args.GetInt("age").Should().Be(42);
        args.GetBool("active").Should().BeTrue();
    }

    [Test]
    public void ConvertArguments_NullLiteral()
    {
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(name: null) { id } }"), null);
        args.GetString("name").Should().BeNull();
    }

    [Test]
    public void ConvertArguments_VariableReference_ResolvesFromVariables()
    {
        var variables = new Dictionary<string, object?> { ["skip"] = 5 };
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(skip: $skip) { id } }"), variables);

        args.GetInt("skip").Should().Be(5);
    }

    [Test]
    public void ConvertArguments_VariableReference_JsonElementNormalized()
    {
        using var doc = JsonDocument.Parse("{ \"skip\": 7, \"active\": true, \"name\": \"bob\" }");
        var variables = new Dictionary<string, object?>
        {
            ["skip"] = doc.RootElement.GetProperty("skip"),
            ["active"] = doc.RootElement.GetProperty("active"),
            ["name"] = doc.RootElement.GetProperty("name")
        };

        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(skip: $skip, active: $active, name: $name) { id } }"), variables);

        args.GetInt("skip").Should().Be(7);
        args.GetBool("active").Should().BeTrue();
        args.GetString("name").Should().Be("bob");
    }

    [Test]
    public void ConvertArguments_NoArguments_ReturnsEmpty()
    {
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people { id } }"), null);
        args.Should().BeEmpty();
    }

    [Test]
    public void ConvertArguments_FloatLiteral_UsesInvariantCultureRegardlessOfCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // de-DE uses ',' as the decimal separator and '.' as a thousands separator; the literal must still parse as 9.99, not 999.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(price: 9.99) { id } }"), null);

            args["price"].Should().Be(9.99m); // decimal, not double - see ConvertArguments_FloatLiteral_PrefersDecimalForPrecision.
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void ConvertArguments_FloatLiteral_PrefersDecimalForPrecision()
    {
        // Regression: ParseFloat previously always parsed to double, silently truncating precision beyond double's ~15-17 significant digits for a decimal-typed filter
        // field. decimal.TryParse is tried first (retaining full base-10 precision), falling back to double only when the literal exceeds decimal's range.
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(price: 123456789012345.678) { id } }"), null);

        args["price"].Should().Be(123456789012345.678m);
    }

    [Test]
    public void ConvertArguments_FloatLiteral_OutOfDecimalRange_FallsBackToDouble()
    {
        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(price: 1e300) { id } }"), null);

        args["price"].Should().Be(1e300);
    }

    [Test]
    public void ConvertArguments_FloatVariable_JsonElement_PrefersDecimalForPrecision()
    {
        using var doc = JsonDocument.Parse("{ \"price\": 123456789012345.678 }");
        var variables = new Dictionary<string, object?> { ["price"] = doc.RootElement.GetProperty("price") };

        var args = GraphQLValueConverter.ConvertArguments(ParseArguments("{ people(price: $price) { id } }"), variables);

        args["price"].Should().Be(123456789012345.678m);
    }

    [Test]
    public void ConvertArguments_UndefinedVariable_Throws()
    {
        var args = ParseArguments("{ people(skip: $missing) { id } }");

        var act = () => GraphQLValueConverter.ConvertArguments(args, null);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*missing*");
    }

    [Test]
    public void ConvertArguments_IntLiteralExceedsInt64Range_Throws()
    {
        // Longer than long.MaxValue's 19 digits; GraphQL's IntValue grammar has no magnitude limit, but our CLR representation does.
        var args = ParseArguments("{ people(age: 99999999999999999999999999999999999999) { id } }");

        var act = () => GraphQLValueConverter.ConvertArguments(args, null);
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void GetInt_LongOutOfInt32Range_Throws()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["first"] = 5_000_000_000L };

        var act = () => args.GetInt("first");
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void GetInt_LongWithinInt32Range_ReturnsInt()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["first"] = 42L };
        args.GetInt("first").Should().Be(42);
    }

    [Test]
    public void GetInt_NonNumericString_Throws()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["first"] = "abc" };

        var act = () => args.GetInt("first");
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void GetInt_UnsupportedValueType_Throws()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["first"] = true };

        var act = () => args.GetInt("first");
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void GetBool_NonBooleanString_Throws()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["active"] = "yes" };

        var act = () => args.GetBool("active");
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void GetBool_UnsupportedValueType_Throws()
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["active"] = 42 };

        var act = () => args.GetBool("active");
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }
}
