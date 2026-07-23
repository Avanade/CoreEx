using CoreEx.Data;
using CoreEx.Data.GraphQL.Internal;
using CoreEx.Data.GraphQL.Test.Unit.Model;
using System.Text.Json;

namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLEngineTests
{
    private static readonly List<Person> _people =
    [
        new() { Id = 1, Name = "Alice", Age = 30, Address = new Address { Street = "1 Main St", City = "Springfield" } },
        new() { Id = 2, Name = "Bob", Age = 40, Address = new Address { Street = "2 Elm St", City = "Shelbyville" } }
    ];

    private static GraphQLEngine CreateEngine(Action<GraphQLLiteOptions>? configure = null)
    {
        var options = new GraphQLLiteOptions();

        options.AddQuery<Person>("people", PersonQueryArgsConfig.Default, (qa, pa, ct) =>
        {
            IEnumerable<Person> items = _people;
            if (qa?.Filter is not null)
                items = items.Where(p => p.Name!.Contains(qa.Filter.Replace("name eq '", "").TrimEnd('\'')));

            return Task.FromResult<IItemsResult<Person>>(new ItemsResult<Person>(items, pa));
        });

        options.AddGet<Person>("person", (args, ct) =>
        {
            var id = args.GetInt("id");
            return Task.FromResult(_people.FirstOrDefault(p => p.Id == id));
        });

        configure?.Invoke(options);
        return new GraphQLEngine(options);
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_ProjectsNestedSelection()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { id name address { street city } } }");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().NotBeNull();

        var json = result.Data!.Value.GetProperty("people");
        json.GetArrayLength().Should().Be(2);
        json[0].GetProperty("name").GetString().Should().Be("Alice");
        json[0].GetProperty("address").GetProperty("street").GetString().Should().Be("1 Main St");
        json[0].TryGetProperty("age", out _).Should().BeFalse(); // Not selected - should not be present.
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_UnknownFieldProducesError()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { id nonExistentField } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Message.Contains("nonExistentField"));
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_ReturnsSingleItem()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2) { id name } }");

        result.HasErrors.Should().BeFalse();
        var json = result.Data!.Value.GetProperty("person");
        json.GetProperty("id").GetInt32().Should().Be(2);
        json.GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_NotFound_ProducesNotFoundError()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 999) { id name } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("NOT_FOUND"));
    }

    [Test]
    public async Task ExecuteAsync_UnknownRoot_ProducesError()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ widgets { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("UNKNOWN_ROOT"));
    }

    [Test]
    public async Task ExecuteAsync_SyntaxError_ProducesError()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { ");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("SYNTAX_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_MutationOperation_Rejected()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("mutation { people { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("OPERATION_NOT_SUPPORTED"));
    }

    [Test]
    public async Task ExecuteAsync_SchemaField_ReturnsDiscoveryDocument()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ __schema }");

        result.HasErrors.Should().BeFalse();
        var schema = result.Data!.Value.GetProperty("__schema");
        schema.GetProperty("roots").GetProperty("people").GetProperty("kind").GetString().Should().Be("query");
    }

    [Test]
    public async Task GetSchemaAsync_DescribesRegisteredRootsAndFields()
    {
        var engine = CreateEngine();
        var schema = await engine.GetSchemaAsync();

        var roots = schema.GetProperty("roots");
        roots.GetProperty("people").GetProperty("fields").GetProperty("address").GetProperty("street").GetString().Should().Be("String");
        roots.GetProperty("person").GetProperty("kind").GetString().Should().Be("get");
    }
}
