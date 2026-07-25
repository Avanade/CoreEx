using CoreEx.Data;
using CoreEx.Data.GraphQL.Internal;
using CoreEx.Data.GraphQL.Test.Unit.Model;
using CoreEx.Data.Querying;
using System.Text.Json;

namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLEngineTests
{
    private static readonly List<Person> _people =
    [
        new() { Id = 1, Name = "Alice", Age = 30, Address = new Address { Street = "1 Main St", City = "Springfield" } },
        new() { Id = 2, Name = "Bob", Age = 40, Address = new Address { Street = "2 Elm St", City = "Shelbyville" } },
        new() { Id = 3, Name = "Carol", Age = 25, Address = new Address { Street = "3 Oak St", City = "Springfield" } }
    ];

    private static GraphQLEngine CreateEngine(Action<GraphQLLiteOptions>? configure = null)
    {
        var options = new GraphQLLiteOptions();

        options.AddQuery<Person>("people", PersonQueryArgsConfig.Default, (qa, pa, ct) =>
        {
            var parsed = PersonQueryArgsConfig.Default.Parse(qa).ThrowOnError();
            var query = _people.AsQueryable().Where(parsed).OrderBy(parsed);
            var items = new ItemsResult<Person>(query.WithPaging(pa), pa).WithTotalCount(() => query.LongCount());
            return Task.FromResult<IItemsResult<Person>>(items);
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
    public async Task ExecuteAsync_QueryRoot_ProjectsNestedSelectionAsConnectionAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id name address { street city } } } } }");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().NotBeNull();

        var edges = result.Data!.Value.GetProperty("people").GetProperty("edges");
        edges.GetArrayLength().Should().Be(3);
        var firstNode = edges[0].GetProperty("node");
        firstNode.GetProperty("name").GetString().Should().Be("Alice");
        firstNode.GetProperty("address").GetProperty("street").GetString().Should().Be("1 Main St");
        firstNode.TryGetProperty("age", out _).Should().BeFalse(); // Not selected - should not be present.
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_UnknownFieldProducesErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id nonExistentField } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Message.Contains("nonExistentField"));
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_UnknownConnectionFieldProducesErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { nonExistentField } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("UNKNOWN_FIELD"));
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_Where_EqualityShorthand_FiltersItemsAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(where: { name: \"Bob\" }) { edges { node { id name } } } } ");

        result.HasErrors.Should().BeFalse();
        var edges = result.Data!.Value.GetProperty("people").GetProperty("edges");
        edges.GetArrayLength().Should().Be(1);
        edges[0].GetProperty("node").GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_Where_OperatorObject_FiltersItemsAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(where: { name: { startsWith: \"A\" } }) { edges { node { name } } } }");

        result.HasErrors.Should().BeFalse();
        var edges = result.Data!.Value.GetProperty("people").GetProperty("edges");
        edges.GetArrayLength().Should().Be(1);
        edges[0].GetProperty("node").GetProperty("name").GetString().Should().Be("Alice");
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_Where_AndOr_ComposesCorrectlyAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync(
            "{ people(where: { or: [ { name: \"Alice\" }, { and: [ { age: { ge: 40 } } ] } ] }) { edges { node { name } } } }");

        result.HasErrors.Should().BeFalse();
        var edges = result.Data!.Value.GetProperty("people").GetProperty("edges");
        edges.GetArrayLength().Should().Be(2);
        edges.EnumerateArray().Select(e => e.GetProperty("node").GetProperty("name").GetString()).Should().BeEquivalentTo(["Alice", "Bob"]);
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_Where_UnknownField_ProducesFilterParseErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(where: { unknownField: \"x\" }) { edges { node { name } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("FILTER_PARSE_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_OrderBy_OrdersItemsAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(orderBy: [ { age: ASC } ]) { edges { node { name } } } }");

        result.HasErrors.Should().BeFalse();
        var edges = result.Data!.Value.GetProperty("people").GetProperty("edges");
        edges.EnumerateArray().Select(e => e.GetProperty("node").GetProperty("name").GetString()).Should().Equal("Carol", "Alice", "Bob");
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_FirstAfter_PagesForwardWithCorrectPageInfoAsync()
    {
        var engine = CreateEngine();
        var page1 = await engine.ExecuteAsync("{ people(orderBy: [ { age: ASC } ], first: 2) { edges { node { id } cursor } pageInfo { hasNextPage hasPreviousPage startCursor endCursor } } }");

        page1.HasErrors.Should().BeFalse();
        var people1 = page1.Data!.Value.GetProperty("people");
        var edges1 = people1.GetProperty("edges");
        edges1.GetArrayLength().Should().Be(2);
        edges1.EnumerateArray().Select(e => e.GetProperty("node").GetProperty("id").GetInt32()).Should().Equal(3, 1); // Carol (25), Alice (30).

        var pageInfo1 = people1.GetProperty("pageInfo");
        pageInfo1.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
        pageInfo1.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse();

        var endCursor = pageInfo1.GetProperty("endCursor").GetString();

        var page2 = await engine.ExecuteAsync($"{{ people(orderBy: [ {{ age: ASC }} ], first: 2, after: \"{endCursor}\") {{ edges {{ node {{ id }} }} pageInfo {{ hasNextPage hasPreviousPage }} }} }}");

        page2.HasErrors.Should().BeFalse();
        var people2 = page2.Data!.Value.GetProperty("people");
        people2.GetProperty("edges").EnumerateArray().Select(e => e.GetProperty("node").GetProperty("id").GetInt32()).Should().Equal(2); // Bob (40).
        people2.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean().Should().BeFalse();
        people2.GetProperty("pageInfo").GetProperty("hasPreviousPage").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_InvalidCursor_ProducesArgumentErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(after: \"not-a-valid-cursor\") { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_BackwardPagination_IsRejectedAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(last: 2) { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_TotalCount_OnlyComputedWhenRequestedAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id } } totalCount } }");

        result.HasErrors.Should().BeFalse();
        result.Data!.Value.GetProperty("people").GetProperty("totalCount").GetInt64().Should().Be(3);
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_ReturnsSingleItemAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2) { id name } }");

        result.HasErrors.Should().BeFalse();
        var json = result.Data!.Value.GetProperty("person");
        json.GetProperty("id").GetInt32().Should().Be(2);
        json.GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_NotFound_ProducesNotFoundErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 999) { id name } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("NOT_FOUND"));
    }

    [Test]
    public async Task ExecuteAsync_UnknownRoot_ProducesErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ widgets { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("UNKNOWN_ROOT"));
    }

    [Test]
    public async Task ExecuteAsync_SyntaxError_ProducesErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { ");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("SYNTAX_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_MutationOperation_RejectedAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("mutation { people { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("OPERATION_NOT_SUPPORTED"));
    }

    [Test]
    public async Task ExecuteAsync_SchemaField_ReturnsDiscoveryDocumentAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ __schema }");

        result.HasErrors.Should().BeFalse();
        var schema = result.Data!.Value.GetProperty("__schema");
        schema.GetProperty("roots").GetProperty("people").GetProperty("kind").GetString().Should().Be("query");
    }

    [Test]
    public async Task GetSchemaAsync_DescribesRegisteredRootsAndFieldsAsync()
    {
        var engine = CreateEngine();
        var schema = await engine.GetSchemaAsync();

        var roots = schema.GetProperty("roots");
        var peopleFields = roots.GetProperty("people").GetProperty("fields");
        peopleFields.GetProperty("edges").GetProperty("items").GetProperty("node").GetProperty("address").GetProperty("street").GetString().Should().Be("String");
        peopleFields.GetProperty("pageInfo").GetProperty("hasNextPage").GetString().Should().Be("Boolean");
        roots.GetProperty("people").TryGetProperty("where", out _).Should().BeTrue();
        roots.GetProperty("people").TryGetProperty("orderBy", out _).Should().BeTrue();
        roots.GetProperty("person").GetProperty("kind").GetString().Should().Be("get");
    }

    [Test]
    public async Task ExecuteAsync_TypeNameField_ResolvedAtConnectionEdgeAndNodeLevelsAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { __typename edges { __typename node { __typename id address { __typename street } } } } }");

        result.HasErrors.Should().BeFalse();
        var people = result.Data!.Value.GetProperty("people");
        people.GetProperty("__typename").GetString().Should().Be("PersonConnection");

        var edge = people.GetProperty("edges")[0];
        edge.GetProperty("__typename").GetString().Should().Be("PersonEdge");

        var node = edge.GetProperty("node");
        node.GetProperty("__typename").GetString().Should().Be(nameof(Person));
        node.GetProperty("address").GetProperty("__typename").GetString().Should().Be(nameof(Address));
    }

    [Test]
    public async Task ExecuteAsync_TypeNameField_ResolvedForSingleItemRootAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2) { __typename name } }");

        result.HasErrors.Should().BeFalse();
        result.Data!.Value.GetProperty("person").GetProperty("__typename").GetString().Should().Be(nameof(Person));
    }

    [Test]
    public async Task ExecuteAsync_NestedFieldAlias_IsHonoredInResponseAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { personId: id address { streetName: street } } } } }");

        result.HasErrors.Should().BeFalse();
        var first = result.Data!.Value.GetProperty("people").GetProperty("edges")[0].GetProperty("node");
        first.GetProperty("personId").GetInt32().Should().Be(1);
        first.GetProperty("address").GetProperty("streetName").GetString().Should().Be("1 Main St");
        first.TryGetProperty("id", out _).Should().BeFalse();
        first.GetProperty("address").TryGetProperty("street", out _).Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_FragmentSpread_ProducesExplicitErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id ...PersonFields } } } } fragment PersonFields on Person { name }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("FRAGMENTS_NOT_SUPPORTED"));
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_NoSelectionSet_ProducesSelectionRequiredErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2) }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("SELECTION_REQUIRED"));
    }

    [Test]
    public async Task ExecuteAsync_ConnectionNode_NoSelectionSet_ProducesSelectionRequiredErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("SELECTION_REQUIRED"));
        error.Path.Should().Equal("people", "edges", "node");
    }

    [Test]
    public async Task ExecuteAsync_NestedComplexField_NoSelectionSet_ProducesSelectionRequiredErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id address } } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("SELECTION_REQUIRED"));
        error.Path.Should().Equal("people", "edges", "node", "address");
    }

    [Test]
    public async Task ExecuteAsync_AliasedConnectionRoot_UnknownFieldErrorPathUsesAliasesAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ folks: people { nonExistentField } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("UNKNOWN_FIELD"));
        error.Path.Should().Equal("folks", "nonExistentField");
    }

    [Test]
    public async Task ExecuteAsync_AliasedEdgesAndNode_NoSelectionSet_ErrorPathUsesAliasesAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ folks: people { results: edges { item: node } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("SELECTION_REQUIRED"));
        error.Path.Should().Equal("folks", "results", "item");
    }

    [Test]
    public async Task ExecuteAsync_AliasedNestedComplexField_NoSelectionSet_ErrorPathUsesAliasesAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ folks: people { results: edges { item: node { id location: address } } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("SELECTION_REQUIRED"));
        error.Path.Should().Equal("folks", "results", "item", "location");
    }

    [Test]
    public async Task ExecuteAsync_AliasedUnknownFieldInsideNode_ErrorPathUsesAliasesAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ folks: people { results: edges { item: node { bogus: nonExistentField } } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("UNKNOWN_FIELD"));
        error.Path.Should().Equal("folks", "results", "item", "bogus");
    }

    [Test]
    public async Task ExecuteAsync_DuplicateRootAlias_ProducesDuplicateFieldErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { totalCount } people { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("DUPLICATE_FIELD"));
    }

    [Test]
    public async Task ExecuteAsync_DuplicateConnectionFieldAlias_ProducesDuplicateFieldErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { totalCount totalCount } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("DUPLICATE_FIELD"));
        error.Path.Should().Equal("people", "totalCount");
    }

    [Test]
    public async Task ExecuteAsync_DuplicateEdgesFieldAlias_ProducesDuplicateFieldErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { id } node { id } } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("DUPLICATE_FIELD"));
        error.Path.Should().Equal("people", "edges", "node");
    }

    [Test]
    public async Task ExecuteAsync_DuplicatePageInfoFieldAlias_ProducesDuplicateFieldErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { pageInfo { hasNextPage hasNextPage } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("DUPLICATE_FIELD"));
        error.Path.Should().Equal("people", "pageInfo", "hasNextPage");
    }

    [Test]
    public async Task ExecuteAsync_DuplicateNestedFieldAlias_ProducesDuplicateFieldErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { edges { node { address { city city } } } } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single(e => e.Extensions!["code"]!.Equals("DUPLICATE_FIELD"));
        error.Path.Should().Equal("people", "edges", "node", "address", "city");
    }

    [Test]
    public async Task ExecuteAsync_UndefinedVariable_ProducesArgumentErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(first: $first) { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_FirstArgumentOutOfInt32Range_ProducesArgumentErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(first: 5000000000) { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_TotalCountOnly_ReturnsCorrectCountWithoutEdgesOrPageInfoAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people { totalCount } }");

        result.HasErrors.Should().BeFalse();
        var people = result.Data!.Value.GetProperty("people");
        people.GetProperty("totalCount").GetInt64().Should().Be(3);
        people.TryGetProperty("edges", out _).Should().BeFalse();
        people.TryGetProperty("pageInfo", out _).Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsOperationCanceled_PropagatesRatherThanBecomingAnEngineErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("cancelable", (_, ct) => throw new OperationCanceledException(ct)));

        var act = () => engine.ExecuteAsync("{ cancelable(id: 1) { id } }");
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
