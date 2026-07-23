using CoreEx.Data.GraphQL.Internal;
using GraphQLParser;
using GraphQLParser.AST;
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
}
