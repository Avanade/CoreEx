using CoreEx.Data.GraphQL.Internal;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLFilterTranslatorTests
{
    [Test]
    public void Translate_Null_ReturnsNull() => GraphQLFilterTranslator.Translate(null).Should().BeNull();

    [Test]
    public void Translate_BareScalarShorthand_TranslatesToEquality()
    {
        var where = new Dictionary<string, object?> { ["sku"] = "ABC" };
        GraphQLFilterTranslator.Translate(where).Should().Be("sku eq 'ABC'");
    }

    [TestCase("eq", "sku eq 'ABC'")]
    [TestCase("ne", "sku ne 'ABC'")]
    [TestCase("gt", "sku gt 'ABC'")]
    [TestCase("ge", "sku ge 'ABC'")]
    [TestCase("lt", "sku lt 'ABC'")]
    [TestCase("le", "sku le 'ABC'")]
    [TestCase("startsWith", "startswith(sku, 'ABC')")]
    [TestCase("endsWith", "endswith(sku, 'ABC')")]
    [TestCase("contains", "contains(sku, 'ABC')")]
    public void Translate_OperatorObject_TranslatesToExpectedODataClause(string op, string expected)
    {
        var where = new Dictionary<string, object?> { ["sku"] = new Dictionary<string, object?> { [op] = "ABC" } };
        GraphQLFilterTranslator.Translate(where).Should().Be(expected);
    }

    [Test]
    public void Translate_InOperator_TranslatesToParenthesizedValueList()
    {
        var where = new Dictionary<string, object?> { ["sku"] = new Dictionary<string, object?> { ["in"] = new List<object?> { "A", "B" } } };
        GraphQLFilterTranslator.Translate(where).Should().Be("sku in ('A', 'B')");
    }

    [Test]
    public void Translate_MultipleFields_JoinsWithAnd()
    {
        var where = new Dictionary<string, object?> { ["sku"] = "A", ["category"] = "Widgets" };
        var result = GraphQLFilterTranslator.Translate(where);
        result.Should().Be("(sku eq 'A' and category eq 'Widgets')");
    }

    [Test]
    public void Translate_MultipleOperatorsOnSameField_JoinsWithAnd()
    {
        var where = new Dictionary<string, object?> { ["age"] = new Dictionary<string, object?> { ["ge"] = 18, ["le"] = 65 } };
        GraphQLFilterTranslator.Translate(where).Should().Be("(age ge 18 and age le 65)");
    }

    [Test]
    public void Translate_And_ComposesNestedClauses()
    {
        var where = new Dictionary<string, object?>
        {
            ["and"] = new List<object?>
            {
                new Dictionary<string, object?> { ["sku"] = "A" },
                new Dictionary<string, object?> { ["category"] = "Widgets" }
            }
        };

        GraphQLFilterTranslator.Translate(where).Should().Be("(sku eq 'A' and category eq 'Widgets')");
    }

    [Test]
    public void Translate_Or_ComposesNestedClauses()
    {
        var where = new Dictionary<string, object?>
        {
            ["or"] = new List<object?>
            {
                new Dictionary<string, object?> { ["sku"] = "A" },
                new Dictionary<string, object?> { ["sku"] = "B" }
            }
        };

        GraphQLFilterTranslator.Translate(where).Should().Be("(sku eq 'A' or sku eq 'B')");
    }

    [Test]
    public void Translate_Not_NegatesNestedClause()
    {
        var where = new Dictionary<string, object?> { ["not"] = new Dictionary<string, object?> { ["sku"] = "A" } };
        GraphQLFilterTranslator.Translate(where).Should().Be("not (sku eq 'A')");
    }

    [Test]
    public void Translate_StringValue_EscapesEmbeddedQuotes()
    {
        var where = new Dictionary<string, object?> { ["name"] = "O'Brien" };
        GraphQLFilterTranslator.Translate(where).Should().Be("name eq 'O''Brien'");
    }

    [Test]
    public void Translate_BooleanValue_LowerCases()
    {
        var where = new Dictionary<string, object?> { ["active"] = true };
        GraphQLFilterTranslator.Translate(where).Should().Be("active eq true");
    }

    [Test]
    public void Translate_NumericValue_PassesThrough()
    {
        var where = new Dictionary<string, object?> { ["age"] = 42 };
        GraphQLFilterTranslator.Translate(where).Should().Be("age eq 42");
    }

    [Test]
    public void Translate_UnknownOperator_ThrowsTranslationException()
    {
        var where = new Dictionary<string, object?> { ["sku"] = new Dictionary<string, object?> { ["bogus"] = "A" } };
        var act = () => GraphQLFilterTranslator.Translate(where);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*unknown operator*");
    }

    [Test]
    public void Translate_EmptyObject_ThrowsTranslationException()
    {
        var act = () => GraphQLFilterTranslator.Translate(new Dictionary<string, object?>());
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void Translate_AndNotAList_ThrowsTranslationException()
    {
        var where = new Dictionary<string, object?> { ["and"] = "not-a-list" };
        var act = () => GraphQLFilterTranslator.Translate(where);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*must be a list*");
    }
}
