using CoreEx.Data.GraphQL.Internal;
using CoreEx.Data.Querying;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLFilterTranslatorTests
{
    [Test]
    public void Translate_Null_ReturnsNull() => GraphQLFilterTranslator.Translate(null, null).Should().BeNull();

    [Test]
    public void Translate_BareScalarShorthand_TranslatesToEquality()
    {
        var where = new Dictionary<string, object?> { ["sku"] = "ABC" };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("sku eq 'ABC'");
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
        GraphQLFilterTranslator.Translate(where, null).Should().Be(expected);
    }

    [Test]
    public void Translate_InOperator_TranslatesToParenthesizedValueList()
    {
        var where = new Dictionary<string, object?> { ["sku"] = new Dictionary<string, object?> { ["in"] = new List<object?> { "A", "B" } } };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("sku in ('A', 'B')");
    }

    [Test]
    public void Translate_MultipleFields_JoinsWithAnd()
    {
        var where = new Dictionary<string, object?> { ["sku"] = "A", ["category"] = "Widgets" };
        var result = GraphQLFilterTranslator.Translate(where, null);
        result.Should().Be("(sku eq 'A' and category eq 'Widgets')");
    }

    [Test]
    public void Translate_MultipleOperatorsOnSameField_JoinsWithAnd()
    {
        var where = new Dictionary<string, object?> { ["age"] = new Dictionary<string, object?> { ["ge"] = 18, ["le"] = 65 } };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("(age ge 18 and age le 65)");
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

        GraphQLFilterTranslator.Translate(where, null).Should().Be("(sku eq 'A' and category eq 'Widgets')");
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

        GraphQLFilterTranslator.Translate(where, null).Should().Be("(sku eq 'A' or sku eq 'B')");
    }

    [Test]
    public void Translate_Not_NegatesNestedClause()
    {
        var where = new Dictionary<string, object?> { ["not"] = new Dictionary<string, object?> { ["sku"] = "A" } };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("not (sku eq 'A')");
    }

    [Test]
    public void Translate_StringValue_EscapesEmbeddedQuotes()
    {
        var where = new Dictionary<string, object?> { ["name"] = "O'Brien" };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("name eq 'O''Brien'");
    }

    [Test]
    public void Translate_BooleanValue_LowerCases()
    {
        var where = new Dictionary<string, object?> { ["active"] = true };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("active eq true");
    }

    [Test]
    public void Translate_NumericValue_PassesThrough()
    {
        var where = new Dictionary<string, object?> { ["age"] = 42 };
        GraphQLFilterTranslator.Translate(where, null).Should().Be("age eq 42");
    }

    [Test]
    public void Translate_UnknownOperator_ThrowsTranslationException()
    {
        var where = new Dictionary<string, object?> { ["sku"] = new Dictionary<string, object?> { ["bogus"] = "A" } };
        var act = () => GraphQLFilterTranslator.Translate(where, null);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*unknown operator*");
    }

    [Test]
    public void Translate_EmptyObject_ThrowsTranslationException()
    {
        var act = () => GraphQLFilterTranslator.Translate(new Dictionary<string, object?>(), null);
        act.Should().Throw<GraphQLArgumentTranslationException>();
    }

    [Test]
    public void Translate_AndNotAList_ThrowsTranslationException()
    {
        var where = new Dictionary<string, object?> { ["and"] = "not-a-list" };
        var act = () => GraphQLFilterTranslator.Translate(where, null);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*must be a list*");
    }

    private static QueryFilterParser CreateFilterParser() => QueryArgsConfig.Create()
        .WithFilter(f => f
            .AddField<string>("Sku")
            .AddField<Guid>("Id")
            .AddField<DateTime>("CreatedOn")
            .AddField<DateOnly>("StartDate")
            .AddField<TimeOnly>("StartTime")
            .AddField<TimeSpan>("Duration")
            .AddField<char>("Grade"))
        .FilterParser;

    // Regression: every QueryFilterFieldType.Other-classified type (anything that isn't string/bool/Enum) has no native GraphQL scalar and is always presented as GraphQL
    // String, but QueryFilterFieldConfigBase.ValidateConstant rejects a quoted Literal for FieldType.Other - it requires an unquoted Value token. Guid/DateTime are covered by
    // their own dedicated tests above; these cover the remaining IParsable<T> "Other" types. (Uri, also FieldType.Other per the introspection scalar mapping, is deliberately
    // NOT included here - Uri does not implement IParsable<Uri>, so QueryFilterParser.AddField<Uri> cannot compile; it is never actually reachable as a filter field at all.)
    [TestCase("startDate", "2024-06-15")]
    [TestCase("startTime", "13:45:00")]
    [TestCase("duration", "01:30:00")]
    [TestCase("grade", "A")]
    public void Translate_OtherIParsableTypeField_EmitsUnquoted(string graphQLFieldName, string value)
    {
        var where = new Dictionary<string, object?> { [graphQLFieldName] = value };
        GraphQLFilterTranslator.Translate(where, CreateFilterParser()).Should().Be($"{graphQLFieldName} eq {value}");
    }

    [Test]
    public void Translate_StringField_WithParser_StillQuotes()
    {
        // Regression guard: a genuine String/Enum-typed field must keep quoting even once the parser is supplied.
        var where = new Dictionary<string, object?> { ["sku"] = "ABC" };
        GraphQLFilterTranslator.Translate(where, CreateFilterParser()).Should().Be("sku eq 'ABC'");
    }

    [Test]
    public void Translate_GuidField_EmitsUnquoted()
    {
        // Regression: Guid has no native GraphQL scalar and is always presented as GraphQL String, but the underlying QueryFilterParser rejects a quoted Literal for a
        // Guid-typed (FieldType.Other) field - it requires an unquoted Value token.
        var where = new Dictionary<string, object?> { ["id"] = "affe1234-5717-4562-b3fc-2c963f66afa6" };
        GraphQLFilterTranslator.Translate(where, CreateFilterParser()).Should().Be("id eq affe1234-5717-4562-b3fc-2c963f66afa6");
    }

    [Test]
    public void Translate_DateTimeField_OperatorObject_EmitsUnquoted()
    {
        var where = new Dictionary<string, object?> { ["createdOn"] = new Dictionary<string, object?> { ["gt"] = "2024-01-01T00:00:00Z" } };
        GraphQLFilterTranslator.Translate(where, CreateFilterParser()).Should().Be("createdOn gt 2024-01-01T00:00:00Z");
    }

    [Test]
    public void Translate_UnknownField_WithParser_StillDefaultsToQuoting()
    {
        // Defense in depth is unchanged: an unknown field's value still quotes (the underlying QueryFilterParser rejects the unknown field itself, safely, regardless).
        var where = new Dictionary<string, object?> { ["unknownField"] = "2024-01-01" };
        GraphQLFilterTranslator.Translate(where, CreateFilterParser()).Should().Be("unknownField eq '2024-01-01'");
    }

    [TestCase("2024-01-01 or 1 eq 1")]
    [TestCase("2024-01-01(x)")]
    [TestCase("2024-01-01,x")]
    public void Translate_OtherTypeField_ValueContainsUnsafeCharacter_ThrowsTranslationException(string value)
    {
        // Injection-safety guard: emitting an Other-typed field's value unquoted is only safe because a value containing a space/paren/comma - the exact characters
        // QueryFilterParser's tokenizer uses as bare-token boundaries - is rejected outright, rather than risking it being scanned as more than one token (e.g. injecting
        // an extra "or 1 eq 1" clause).
        var where = new Dictionary<string, object?> { ["createdOn"] = new Dictionary<string, object?> { ["gt"] = value } };
        var act = () => GraphQLFilterTranslator.Translate(where, CreateFilterParser());
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*must not contain spaces, parentheses, or commas*");
    }
}
