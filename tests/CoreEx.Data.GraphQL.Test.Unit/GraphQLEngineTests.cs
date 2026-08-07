using CoreEx;
using CoreEx.Data;
using CoreEx.Data.GraphQL.Internal;
using CoreEx.Data.GraphQL.Test.Unit.Model;
using CoreEx.Data.Querying;
using CoreEx.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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
        var options = new GraphQLLiteOptions { EnableIntrospection = true }; // Defaults to false; enabled here so the existing introspection-focused tests keep exercising it unmodified.

        options.AddQuery<Person>("people", PersonQueryArgsConfig.Default, (qa, pa, ct) =>
        {
            var parsed = PersonQueryArgsConfig.Default.Parse(qa).ThrowOnError();
            var query = _people.AsQueryable().Where(parsed).OrderBy(parsed);
            var items = new ItemsResult<Person>(query.WithPaging(pa), pa).WithTotalCount(() => query.LongCount());
            return Task.FromResult<IItemsResult<Person>>(items);
        });

        options.AddGet<Person>("person", (args, ct) =>
        {
            var id = args.Arguments.GetInt("id");
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
    public async Task ExecuteAsync_ItemRoot_MalformedIncludeTextArgument_ProducesArgumentErrorRatherThanThrowingAsync()
    {
        // 'includeText' argument-shape translation (GraphQLArgsMapper.ApplyItemRootFlags) happens outside the resolver invocation for item roots; it must still be
        // captured by the same try/catch so a bad value is reported as a per-field ARGUMENT_ERROR rather than escaping ExecuteAsync and aborting the whole request.
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2, includeText: \"not-a-boolean\") { id name } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_NullableComplexStructProperty_RecursesIntoUnderlyingPropertiesAsync()
    {
        // Money? (Nullable<Money>) must be shaped from the underlying Money struct's own properties, not Nullable<T>'s HasValue/Value - otherwise 'amount'/'currency'
        // would be unresolvable and produce spurious UNKNOWN_FIELD errors.
        var engine = CreateEngine(options => options.AddGet<Invoice>("invoice", (_, _) => Task.FromResult<Invoice?>(new Invoice { Id = 1, Number = "INV-1", Total = new Money(100m, "USD") })));

        var result = await engine.ExecuteAsync("{ invoice(id: 1) { number total { amount currency } } }");

        result.HasErrors.Should().BeFalse();
        var invoice = result.Data!.Value.GetProperty("invoice");
        invoice.GetProperty("number").GetString().Should().Be("INV-1");
        var total = invoice.GetProperty("total");
        total.GetProperty("amount").GetDecimal().Should().Be(100m);
        total.GetProperty("currency").GetString().Should().Be("USD");
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
    public async Task ExecuteAsync_SchemaField_ReturnsSpecCompliantIntrospectionAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ __schema { queryType { name } types { name kind } } }");

        result.HasErrors.Should().BeFalse();
        var schema = result.Data!.Value.GetProperty("__schema");
        schema.GetProperty("queryType").GetProperty("name").GetString().Should().Be("Query");

        var typeNames = schema.GetProperty("types").EnumerateArray().Select(t => t.GetProperty("name").GetString()).ToList();
        typeNames.Should().Contain(["Query", "Person", "PersonConnection", "PersonEdge", "PageInfo", "String", "Int", "Boolean", "ID", "JSON", "Long",
            "PersonWhereInput", "PersonOrderByInput", "StringFilterInput", "IntFilterInput", "SortDirection"]);

        var queryType = FindType(schema, "Query");
        var peopleField = FindField(queryType, "people");
        peopleField.GetProperty("type").GetProperty("ofType").GetProperty("name").GetString().Should().Be("PersonConnection");
        var peopleArgs = peopleField.GetProperty("args").EnumerateArray().ToList();
        peopleArgs.Select(a => a.GetProperty("name").GetString()).Should().Contain(["first", "after", "where", "orderBy", "includeText", "includeInactive"]);

        var whereArgType = peopleArgs.Single(a => a.GetProperty("name").GetString() == "where").GetProperty("type");
        whereArgType.GetProperty("kind").GetString().Should().Be("INPUT_OBJECT");
        whereArgType.GetProperty("name").GetString().Should().Be("PersonWhereInput");

        // 'orderBy' is a nullable list of non-null PersonOrderByInput items.
        var orderByArgType = peopleArgs.Single(a => a.GetProperty("name").GetString() == "orderBy").GetProperty("type");
        orderByArgType.GetProperty("kind").GetString().Should().Be("LIST");
        orderByArgType.GetProperty("ofType").GetProperty("kind").GetString().Should().Be("NON_NULL");
        orderByArgType.GetProperty("ofType").GetProperty("ofType").GetProperty("name").GetString().Should().Be("PersonOrderByInput");

        var whereInput = FindType(schema, "PersonWhereInput");
        var whereInputFields = whereInput.GetProperty("inputFields").EnumerateArray().ToList();
        whereInputFields.Select(f => f.GetProperty("name").GetString()).Should().Contain(["and", "or", "not", "name", "age"]);
        whereInputFields.Single(f => f.GetProperty("name").GetString() == "name").GetProperty("type").GetProperty("name").GetString().Should().Be("StringFilterInput");
        whereInputFields.Single(f => f.GetProperty("name").GetString() == "age").GetProperty("type").GetProperty("name").GetString().Should().Be("IntFilterInput");

        var orderByInput = FindType(schema, "PersonOrderByInput");
        var orderByInputFields = orderByInput.GetProperty("inputFields").EnumerateArray().ToList();
        orderByInputFields.Select(f => f.GetProperty("name").GetString()).Should().Contain(["name", "age"]);
        orderByInputFields.Single(f => f.GetProperty("name").GetString() == "name").GetProperty("type").GetProperty("name").GetString().Should().Be("SortDirection");

        var sortDirection = FindType(schema, "SortDirection");
        sortDirection.GetProperty("kind").GetString().Should().Be("ENUM");
        sortDirection.GetProperty("enumValues").EnumerateArray().Select(v => v.GetProperty("name").GetString()).Should().BeEquivalentTo(["ASC", "DESC"]);

        // Person implements IReadOnlyIdentifier<int>, so the 'person' get-root should advertise a required 'id: ID!' argument, plus 'includeText' (honoured) and 'includeInactive'
        // (advertised for consistency with list roots, but a no-op for a single-item get).
        var personField = FindField(queryType, "person");
        var personArgs = personField.GetProperty("args").EnumerateArray().ToList();
        personArgs.Select(a => a.GetProperty("name").GetString()).Should().BeEquivalentTo(["id", "includeText", "includeInactive"]);
        var idArgType = personArgs.Single(a => a.GetProperty("name").GetString() == "id").GetProperty("type");
        idArgType.GetProperty("kind").GetString().Should().Be("NON_NULL");
        idArgType.GetProperty("ofType").GetProperty("name").GetString().Should().Be("ID");
    }

    [Test]
    public async Task ExecuteAsync_TypeField_ReturnsNamedTypeAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ __type(name: \"Person\") { name kind fields { name } } }");

        result.HasErrors.Should().BeFalse();
        var type = result.Data!.Value.GetProperty("__type");
        type.GetProperty("name").GetString().Should().Be("Person");
        type.GetProperty("kind").GetString().Should().Be("OBJECT");
        type.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString()).Should().Contain(["id", "name", "age", "address"]);
    }

    [Test]
    public async Task ExecuteAsync_TypeField_UnknownNameReturnsNullAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ __type(name: \"DoesNotExist\") { name } }");

        result.HasErrors.Should().BeFalse();
        result.Data!.Value.GetProperty("__type").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task GetSchemaAsync_ReturnsSameSpecCompliantDocumentAsSchemaFieldAsync()
    {
        var engine = CreateEngine();
        var schema = await engine.GetSchemaAsync();

        schema.GetProperty("queryType").GetProperty("name").GetString().Should().Be("Query");
        schema.GetProperty("types").EnumerateArray().Select(t => t.GetProperty("name").GetString()).Should().Contain("Person");
    }

    /// <summary>
    /// Finds a named type within an already-materialized <c>__Schema.types</c> array.
    /// </summary>
    private static JsonElement FindType(JsonElement schema, string name) => schema.GetProperty("types").EnumerateArray().Single(t => t.GetProperty("name").GetString() == name);

    /// <summary>
    /// Finds a named field within an already-materialized <c>__Type.fields</c> array.
    /// </summary>
    private static JsonElement FindField(JsonElement type, string name) => type.GetProperty("fields").EnumerateArray().Single(f => f.GetProperty("name").GetString() == name);

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

    private static GraphQLEngine CreateEngineWithIdentifierRoot()
    {
        var options = new GraphQLLiteOptions();
        options.AddGet<Person>("personByIdentifier", (args, ct) => Task.FromResult(_people.FirstOrDefault(p => p.Id == args.GetIdentifier<int>())));
        return new GraphQLEngine(options);
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_GetIdentifier_LiteralArgument_SucceedsAsync()
    {
        var engine = CreateEngineWithIdentifierRoot();
        var result = await engine.ExecuteAsync("{ personByIdentifier(id: 2) { name } }");

        result.HasErrors.Should().BeFalse();
        result.Data!.Value.GetProperty("personByIdentifier").GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_GetIdentifier_VariableSuppliedArgument_SucceedsAsync()
    {
        // Regression: a literal Int argument boxes as `int` (GraphQLValueConverter.ParseInt), but a variable-supplied Int arriving via a real JSON request body boxes as
        // `long` (FromJsonElement always tries TryGetInt64 first) - GetIdentifier<int>() previously did a strict `is TId` cast with no widening, so this identical
        // logical request only failed when supplied via a variable, exactly how a real GraphQL client (Apollo, Relay, GraphiQL's variables panel) sends parameterized requests.
        using var doc = JsonDocument.Parse("{ \"id\": 2 }");
        var variables = new Dictionary<string, object?> { ["id"] = doc.RootElement.GetProperty("id") };

        var engine = CreateEngineWithIdentifierRoot();
        var result = await engine.ExecuteAsync("query($id: Int) { personByIdentifier(id: $id) { name } }", variables: variables);

        result.HasErrors.Should().BeFalse();
        result.Data!.Value.GetProperty("personByIdentifier").GetProperty("name").GetString().Should().Be("Bob");
    }

    [Test]
    public async Task ExecuteAsync_PageInfo_MixedCaseFieldName_ResolvesCaseInsensitivelyAsync()
    {
        // Regression: the four PageInfo field names (hasNextPage/hasPreviousPage/startCursor/endCursor) were matched case-sensitively, inconsistent with every other field
        // name in the schema (edges/node/cursor/pageInfo/totalCount, and every DTO field), which are all explicitly case-insensitive by design.
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(first: 2) { pageInfo { HasNextPage hasPreviousPage } } }");

        result.HasErrors.Should().BeFalse();
        var pageInfo = result.Data!.Value.GetProperty("people").GetProperty("pageInfo");
        pageInfo.GetProperty("HasNextPage").GetBoolean().Should().BeTrue();
        pageInfo.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse();
    }

    /// <summary>
    /// A distinct CLR type that deliberately shares its simple name ("Person") with <see cref="Model.Person"/> but lives in a different (nested) scope, used to prove the
    /// introspection schema builder detects and rejects a genuine type-name collision rather than silently reusing the first-registered type's shape for both.
    /// </summary>
    private static class OtherNamespace
    {
        public class Person : IReadOnlyIdentifier<int>
        {
            public int Id { get; set; }
        }
    }

    [Test]
    public void GetSchemaAsync_TwoDistinctTypesShareSimpleName_ThrowsInvalidOperationException()
    {
        var options = new GraphQLLiteOptions { EnableIntrospection = true };
        options.AddGet<Person>("person", (args, ct) => Task.FromResult<Person?>(_people[0]));
        options.AddGet<OtherNamespace.Person>("otherPerson", (args, ct) => Task.FromResult<OtherNamespace.Person?>(null));

        var engine = new GraphQLEngine(options);
        var act = async () => await engine.GetSchemaAsync();

        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*'Person'*already registered*");
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

    [Test]
    public async Task ExecuteAsync_ResolverThrowsArgumentException_MapsToArgumentErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("missingArg", (_, _) => throw new ArgumentException("'id' argument is required.")));

        var result = await engine.ExecuteAsync("{ missingArg(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsKeyNotFoundException_MapsToArgumentErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("missingKey", (args, _) => throw new KeyNotFoundException("The given key 'id' was not present.")));

        var result = await engine.ExecuteAsync("{ missingKey(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsUnmappedException_MapsToExecutionErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("faulty", (_, _) => throw new InvalidOperationException("Something went wrong.")));

        var result = await engine.ExecuteAsync("{ faulty(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("EXECUTION_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsConflictException_MapsToConflictErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("conflict", (_, _) => throw new ConflictException("Already exists.")));

        var result = await engine.ExecuteAsync("{ conflict(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("CONFLICT_ERROR");
        error.Message.Should().Be("Already exists.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsDuplicateException_MapsToDuplicateErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("duplicate", (_, _) => throw new DuplicateException("Duplicate detected.")));

        var result = await engine.ExecuteAsync("{ duplicate(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("DUPLICATE_ERROR");
        error.Message.Should().Be("Duplicate detected.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsConcurrencyException_MapsToConcurrencyErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("concurrency", (_, _) => throw new ConcurrencyException("Concurrency conflict.")));

        var result = await engine.ExecuteAsync("{ concurrency(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("CONCURRENCY_ERROR");
        error.Message.Should().Be("Concurrency conflict.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsAuthenticationException_MapsToAuthenticationErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("authn", (_, _) => throw new AuthenticationException("Not authenticated.")));

        var result = await engine.ExecuteAsync("{ authn(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("AUTHENTICATION_ERROR");
        error.Message.Should().Be("Not authenticated.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsAuthorizationException_MapsToAuthorizationErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("authz", (_, _) => throw new AuthorizationException("Not authorized.")));

        var result = await engine.ExecuteAsync("{ authz(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("AUTHORIZATION_ERROR");
        error.Message.Should().Be("Not authorized.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsBusinessException_MapsToBusinessErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("business", (_, _) => throw new BusinessException("Business rule violated.")));

        var result = await engine.ExecuteAsync("{ business(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("BUSINESS_ERROR");
        error.Message.Should().Be("Business rule violated.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsUnmappedExtendedException_MapsToGenericExecutionErrorAsync()
    {
        // TransientException is a real IExtendedException not individually special-cased in MapException - it must still fall through to the generic
        // IExtendedException branch (EXECUTION_ERROR, surfacing its own safe message) rather than the catch-all unexpected-exception path.
        var engine = CreateEngine(options => options.AddGet<Person>("transient", (_, _) => throw new TransientException("Try again later.")));

        var result = await engine.ExecuteAsync("{ transient(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("EXECUTION_ERROR");
        error.Message.Should().Be("Try again later.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsValidationExceptionWithMessages_IncludesPerPropertyMessagesExtensionAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("invalid", (_, _) => throw new ValidationException(
        [
            MessageItem.CreateErrorMessage("name", "Name is required."),
            MessageItem.CreateErrorMessage("age", "Age must be greater than zero."),
            MessageItem.CreateErrorMessage("age", "Age must be a whole number.")
        ])));

        var result = await engine.ExecuteAsync("{ invalid(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("VALIDATION_ERROR");

        var messages = error.Extensions!["messages"].Should().BeOfType<Dictionary<string, string[]>>().Subject;
        messages["name"].Should().Equal("Name is required.");
        messages["age"].Should().Equal("Age must be greater than zero.", "Age must be a whole number.");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsValidationExceptionWithoutMessages_ProducesPlainValidationErrorAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Person>("invalid", (_, _) => throw new ValidationException("Overall validation failure.")));

        var result = await engine.ExecuteAsync("{ invalid(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        var error = result.Errors!.Single();
        error.Extensions!["code"].Should().Be("VALIDATION_ERROR");
        error.Message.Should().Be("Overall validation failure.");
        error.Extensions!.Should().NotContainKey("messages");
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsKnownExtendedException_LogsOnlyWhenShouldBeLoggedIsTrueAsync()
    {
        var provider = new TestLoggerProvider();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(provider)).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
        try
        {
            var quietEngine = CreateEngine(options => options.AddGet<Person>("quiet", (_, _) => throw new ConflictException("Quiet by default.")));
            await quietEngine.ExecuteAsync("{ quiet(id: 1) { id } }");
            provider.Entries.Should().BeEmpty("ConflictException.ShouldBeLogged defaults to false, matching the REST WebApi convention for known/expected exceptions");

            var loudEngine = CreateEngine(options => options.AddGet<Person>("loud", (_, _) => throw new ConflictException("Explicitly opted in to logging.") { ShouldBeLogged = true }));
            await loudEngine.ExecuteAsync("{ loud(id: 1) { id } }");
            provider.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && e.Exception is ConflictException);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_SensitiveDataLoggingDisabledByDefault_LogsStructuralInfoOnlyAsync()
    {
        var provider = new TestLoggerProvider();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(provider).SetMinimumLevel(LogLevel.Debug)).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
        try
        {
            var engine = CreateEngine();
            await engine.ExecuteAsync("{ people(where: { name: \"Bob\" }) { edges { node { id } } } }");

            provider.Entries.Should().ContainSingle(e => e.Message.Contains("HasFilter=True"));
            provider.Entries.Should().NotContain(e => e.Message.Contains("Bob"), "the literal filter text must not be logged unless EnableSensitiveDataLogging is opted into");
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecuteAsync_QueryRoot_EnableSensitiveDataLogging_LogsLiteralFilterTextAsync()
    {
        var provider = new TestLoggerProvider();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(provider).SetMinimumLevel(LogLevel.Debug)).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
        try
        {
            var engine = CreateEngine(options => options.EnableSensitiveDataLogging = true);
            await engine.ExecuteAsync("{ people(where: { name: \"Bob\" }) { edges { node { id } } } }");

            provider.Entries.Should().ContainSingle(e => e.Message.Contains("name eq 'Bob'"));
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsUnmappedException_AlwaysLogsAndSurfacesGenericMessageByDefaultAsync()
    {
        var provider = new TestLoggerProvider();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(provider)).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
        try
        {
            var engine = CreateEngine(options => options.AddGet<Person>("faulty", (_, _) => throw new InvalidOperationException("Something went wrong.")));
            var result = await engine.ExecuteAsync("{ faulty(id: 1) { id } }");

            var error = result.Errors!.Single();
            error.Extensions!["code"].Should().Be("EXECUTION_ERROR");
            error.Message.Should().NotBe("Something went wrong.", "the real message must not leak by default - it must be a generic UnexpectedInternalException message");
            provider.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException,
                "unlike known/expected IExtendedException types, a genuinely unexpected exception is always logged");
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecuteAsync_ResolverThrowsUnmappedException_SurfacesRealMessageWhenIncludeExceptionInProblemDetailsEnabledAsync()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:IncludeExceptionInProblemDetails"] = "true" }).Build();
        var services = new ServiceCollection().AddSingleton<IConfiguration>(config).BuildServiceProvider();
        ExecutionContext.SetCurrent(new ExecutionContext { ServiceProvider = services });
        try
        {
            var engine = CreateEngine(options => options.AddGet<Person>("faulty", (_, _) => throw new InvalidOperationException("Something went wrong.")));
            var result = await engine.ExecuteAsync("{ faulty(id: 1) { id } }");

            result.Errors!.Single().Message.Should().Be("Something went wrong.");
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecuteAsync_TooManyRootFields_ProducesErrorWithoutInvokingAnyResolverAsync()
    {
        var invoked = false;
        var engine = CreateEngine(options =>
        {
            options.MaxRootFields = 1;
            options.AddGet<Person>("extra", (_, _) => { invoked = true; return Task.FromResult<Person?>(null); });
        });

        var result = await engine.ExecuteAsync("{ a: person(id: 1) { id } b: extra(id: 1) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("TOO_MANY_ROOT_FIELDS"));
        result.Data.Should().BeNull();
        invoked.Should().BeFalse("MaxRootFields must fail fast before any backend work is performed");
    }

    [Test]
    public async Task ExecuteAsync_MaxRootFieldsUnset_AllowsAnyNumberOfRootFieldsAsync()
    {
        var engine = CreateEngine(); // MaxRootFields defaults to null (unlimited).
        var result = await engine.ExecuteAsync("{ a: person(id: 1) { id } b: person(id: 2) { id } c: person(id: 3) { id } }");

        result.HasErrors.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_IntrospectionDisabled_SchemaFieldProducesErrorAndNullDataAsync()
    {
        var engine = CreateEngine(options => options.EnableIntrospection = false);
        var result = await engine.ExecuteAsync("{ __schema { queryType { name } } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("INTROSPECTION_DISABLED"));
        result.Data!.Value.GetProperty("__schema").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task ExecuteAsync_IntrospectionDisabled_TypeFieldProducesErrorAndNullDataAsync()
    {
        var engine = CreateEngine(options => options.EnableIntrospection = false);
        var result = await engine.ExecuteAsync("{ __type(name: \"Person\") { name } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("INTROSPECTION_DISABLED"));
        result.Data!.Value.GetProperty("__type").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task GetSchemaAsync_IsUnaffectedByEnableIntrospectionFalseAsync()
    {
        // GetSchemaAsync (the direct API) is a server-side escape hatch (e.g. internal tooling) - only the client-facing __schema/__type query fields are gated by EnableIntrospection.
        var engine = CreateEngine(options => options.EnableIntrospection = false);
        var schema = await engine.GetSchemaAsync();

        schema.GetProperty("queryType").GetProperty("name").GetString().Should().Be("Query");
    }

    [Test]
    public async Task ExecuteAsync_Introspection_DepthCapMatchesRuntimeFieldResolutionCapAsync()
    {
        var engine = CreateEngine(options => options.AddGet<Depth0>("depthNode", (_, _) => Task.FromResult<Depth0?>(null)));

        // Depth7 is nested at depth 7 (< GraphQLTypeShape.MaxDepth of 8): introspection should still advertise its 'next' field.
        var depth7Result = await engine.ExecuteAsync("{ __type(name: \"Depth7\") { name fields { name } } }");
        depth7Result.HasErrors.Should().BeFalse();
        depth7Result.Data!.Value.GetProperty("__type").GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString()).Should().Contain("next");

        // Depth8 is nested at depth 8 (no longer < MaxDepth): introspection registers it as an empty-fields stub, matching GraphQLTypeShape.GetFieldMap's own cap exactly - so a client
        // is never told about a field the runtime would then reject as UNKNOWN_FIELD.
        var depth8Result = await engine.ExecuteAsync("{ __type(name: \"Depth8\") { name fields { name } } }");
        depth8Result.HasErrors.Should().BeFalse();
        var depth8Type = depth8Result.Data!.Value.GetProperty("__type");
        depth8Type.GetProperty("name").GetString().Should().Be("Depth8");
        depth8Type.GetProperty("fields").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_WhereArgument_ProducesArgumentErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2, where: { name: \"Bob\" }) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ItemRoot_OrderByArgument_ProducesArgumentErrorAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 2, orderBy: [{ name: DESC }]) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Errors!.Should().ContainSingle(e => e.Extensions!["code"]!.Equals("ARGUMENT_ERROR"));
    }

    [Test]
    public async Task ExecuteAsync_ErroredRootField_NotFound_SetsAliasToNullInDataAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ person(id: 999) { id } }");

        result.HasErrors.Should().BeTrue();
        result.Data!.Value.GetProperty("person").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task ExecuteAsync_ErroredRootField_UnknownRoot_SetsAliasToNullInDataAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ widgets { id } }");

        result.HasErrors.Should().BeTrue();
        result.Data!.Value.GetProperty("widgets").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public async Task ExecuteAsync_ErroredRootField_ArgumentConversionFailure_SetsAliasToNullInDataAsync()
    {
        var engine = CreateEngine();
        var result = await engine.ExecuteAsync("{ people(first: $first) { edges { node { id } } } }");

        result.HasErrors.Should().BeTrue();
        result.Data!.Value.GetProperty("people").ValueKind.Should().Be(JsonValueKind.Null);
    }

    /// <summary>
    /// A minimal in-memory <see cref="ILoggerProvider"/> used to capture logged entries for the exception-mapping/logging tests above.
    /// </summary>
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        public ConcurrentBag<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new TestLogger(this);

        public void Dispose() { }

        private sealed class TestLogger(TestLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => provider.Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }
}
