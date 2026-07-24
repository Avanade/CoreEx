using CoreEx.Data.GraphQL.Internal;

namespace CoreEx.Data.GraphQL.Test.Unit.Internal;

[TestFixture]
public class GraphQLOrderByTranslatorTests
{
    [Test]
    public void Translate_Null_ReturnsNull() => GraphQLOrderByTranslator.Translate(null).Should().BeNull();

    [Test]
    public void Translate_SingleFieldNoDirection_ReturnsFieldNameOnly()
    {
        var orderBy = new List<object?> { new Dictionary<string, object?> { ["name"] = null } };
        GraphQLOrderByTranslator.Translate(orderBy).Should().Be("name");
    }

    [Test]
    public void Translate_SingleFieldAsc_TranslatesToAsc()
    {
        var orderBy = new List<object?> { new Dictionary<string, object?> { ["name"] = "ASC" } };
        GraphQLOrderByTranslator.Translate(orderBy).Should().Be("name asc");
    }

    [Test]
    public void Translate_SingleFieldDesc_TranslatesToDesc()
    {
        var orderBy = new List<object?> { new Dictionary<string, object?> { ["name"] = "DESC" } };
        GraphQLOrderByTranslator.Translate(orderBy).Should().Be("name desc");
    }

    [Test]
    public void Translate_MultipleFields_PreservesListOrderAsPrecedence()
    {
        var orderBy = new List<object?>
        {
            new Dictionary<string, object?> { ["text"] = "DESC" },
            new Dictionary<string, object?> { ["sku"] = "ASC" }
        };

        GraphQLOrderByTranslator.Translate(orderBy).Should().Be("text desc, sku asc");
    }

    [Test]
    public void Translate_NotAList_ThrowsTranslationException()
    {
        var act = () => GraphQLOrderByTranslator.Translate("not-a-list");
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*must be a list of input objects*");
    }

    [Test]
    public void Translate_EmptyList_ThrowsTranslationException()
    {
        var act = () => GraphQLOrderByTranslator.Translate(new List<object?>());
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*at least one field*");
    }

    [Test]
    public void Translate_InvalidDirectionToken_ThrowsTranslationException()
    {
        var orderBy = new List<object?> { new Dictionary<string, object?> { ["name"] = "SIDEWAYS" } };
        var act = () => GraphQLOrderByTranslator.Translate(orderBy);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*'ASC' or 'DESC'*");
    }

    [Test]
    public void Translate_ItemNotAnObject_ThrowsTranslationException()
    {
        var orderBy = new List<object?> { "not-an-object" };
        var act = () => GraphQLOrderByTranslator.Translate(orderBy);
        act.Should().Throw<GraphQLArgumentTranslationException>().WithMessage("*must be an input object*");
    }
}
