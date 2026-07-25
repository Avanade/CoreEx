namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Builds a spec-compliant GraphQL introspection schema graph (see the <see href="https://spec.graphql.org/October2021/#sec-Introspection">GraphQL introspection specification</see>) for the
/// roots registered on a <see cref="GraphQLLiteOptions"/>, exposed via the <c>__schema</c>/<c>__type</c> meta-fields.
/// </summary>
/// <remarks>The graph is built once (see <see cref="GraphQLEngine"/>'s lazy caching) rather than per-request: it is derived purely from registration-time information (root names, CLR item
/// types, and each query root's item shape), which does not change for the lifetime of a registered <see cref="GraphQLLiteOptions"/> instance.
/// <para>Known simplifications (documented in <c>AGENTS.md</c>/<c>README.md</c>): the <c>where</c>/<c>orderBy</c> query-root arguments are declared as an opaque <c>JSON</c> custom scalar
/// rather than fully-typed <c>INPUT_OBJECT</c> graphs (the runtime OData-esque filter/orderby translation is dynamic and does not derive from a fixed input shape); CLR <see langword="enum"/>
/// and <see cref="IReferenceData"/> properties are described as the <c>String</c> scalar (matching their actual JSON wire representation) rather than a spec <c>ENUM</c> type; and a
/// single-item <c>AddGet</c> root only advertises an <c>id: ID!</c> argument where its registered item <see cref="Type"/> implements <see cref="IReadOnlyIdentifier{TId}"/> - otherwise it
/// advertises no arguments at all, rather than guessing at an argument shape the registration API does not declare.</para></remarks>
internal static class GraphQLIntrospectionSchemaBuilder
{
    private const string QueryTypeName = "Query";
    private const string PageInfoTypeName = "PageInfo";
    private const string JsonScalarName = "JSON";
    private const string LongScalarName = "Long";

    private static readonly string[] _builtInScalars = ["String", "Int", "Float", "Boolean", "ID"];

    /// <summary>
    /// Builds the introspection document (the <c>__Schema</c> object content) and a name-keyed lookup of every named type it contains (for <c>__type(name:)</c>).
    /// </summary>
    /// <param name="options">The <see cref="GraphQLLiteOptions"/> describing the registered roots.</param>
    /// <param name="jsonOptions">The <see cref="JsonSerializerOptions"/> used to derive each root's JSON field names.</param>
    /// <returns>The built <see cref="GraphQLIntrospectionDocument"/>.</returns>
    public static GraphQLIntrospectionDocument Build(GraphQLLiteOptions options, JsonSerializerOptions jsonOptions)
    {
        var types = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

        foreach (var scalar in _builtInScalars)
            EnsureScalar(types, scalar);

        EnsureScalar(types, JsonScalarName);
        EnsureScalar(types, LongScalarName);
        EnsurePageInfoType(types);

        var queryFields = new JsonArray();

        foreach (var root in options.QueryRoots.Values)
            queryFields.Add(BuildQueryRootField(root, types, jsonOptions));

        foreach (var root in options.ItemRoots.Values)
            queryFields.Add(BuildItemRootField(root, types, jsonOptions));

        types[QueryTypeName] = NewObjectType(QueryTypeName, "The root query type.", queryFields);

        var schema = new JsonObject
        {
            ["queryType"] = NamedRef(QueryTypeName, "OBJECT"),
            ["mutationType"] = null,
            ["subscriptionType"] = null,
            ["types"] = new JsonArray([.. types.Values]),
            ["directives"] = new JsonArray()
        };

        return new GraphQLIntrospectionDocument(schema, types);
    }

    /// <summary>
    /// Builds the <c>Query</c> type field descriptor for a registered <see cref="GraphQLQueryRoot"/>, registering its <c>&lt;Item&gt;Edge</c>/<c>&lt;Item&gt;Connection</c> object types.
    /// </summary>
    private static JsonObject BuildQueryRootField(GraphQLQueryRoot root, Dictionary<string, JsonObject> types, JsonSerializerOptions jsonOptions)
    {
        var itemTypeName = EnsureObjectType(types, root.ItemType, jsonOptions);
        var edgeTypeName = $"{itemTypeName}Edge";
        var connectionTypeName = $"{itemTypeName}Connection";

        if (!types.ContainsKey(edgeTypeName))
        {
            types[edgeTypeName] = NewObjectType(edgeTypeName, $"A single {itemTypeName} edge in a {connectionTypeName}.", new JsonArray
            {
                NewField("node", NamedRef(itemTypeName, "OBJECT")),
                NewField("cursor", NonNullOf(NamedRef("String", "SCALAR")))
            });
        }

        if (!types.ContainsKey(connectionTypeName))
        {
            types[connectionTypeName] = NewObjectType(connectionTypeName, $"A Relay Cursor Connection page of {itemTypeName}.", new JsonArray
            {
                NewField("edges", ListOf(NonNullOf(NamedRef(edgeTypeName, "OBJECT")))),
                NewField("pageInfo", NonNullOf(NamedRef(PageInfoTypeName, "OBJECT"))),
                NewField("totalCount", NamedRef(LongScalarName, "SCALAR"))
            });
        }

        var args = new JsonArray
        {
            NewArg("first", NamedRef("Int", "SCALAR")),
            NewArg("after", NamedRef("String", "SCALAR")),
            NewArg("where", NamedRef(JsonScalarName, "SCALAR")),
            NewArg("orderBy", NamedRef(JsonScalarName, "SCALAR")),
            NewArg("includeText", NamedRef("Boolean", "SCALAR")),
            NewArg("includeInactive", NamedRef("Boolean", "SCALAR"))
        };

        return NewField(root.Name, NonNullOf(NamedRef(connectionTypeName, "OBJECT")), args);
    }

    /// <summary>
    /// Builds the <c>Query</c> type field descriptor for a registered <see cref="GraphQLItemRoot"/>, advertising an <c>id: ID!</c> argument only where its item <see cref="Type"/>
    /// implements <see cref="IReadOnlyIdentifier{TId}"/>.
    /// </summary>
    private static JsonObject BuildItemRootField(GraphQLItemRoot root, Dictionary<string, JsonObject> types, JsonSerializerOptions jsonOptions)
    {
        var itemTypeName = EnsureObjectType(types, root.ItemType, jsonOptions);
        var args = new JsonArray();

        if (root.ItemType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyIdentifier<>)))
            args.Add(NewArg("id", NonNullOf(NamedRef("ID", "SCALAR"))));

        return NewField(root.Name, NamedRef(itemTypeName, "OBJECT"), args);
    }

    /// <summary>
    /// Ensures an <c>OBJECT</c> type is registered for the specified <paramref name="clrType"/>, recursing into its <see cref="GraphQLTypeShape"/>-derived complex fields.
    /// </summary>
    /// <remarks>A placeholder is registered <i>before</i> recursing into fields, guarding against infinite recursion for self-referencing (cyclic) DTO graphs.</remarks>
    private static string EnsureObjectType(Dictionary<string, JsonObject> types, Type clrType, JsonSerializerOptions jsonOptions)
    {
        var typeName = clrType.Name;
        if (types.ContainsKey(typeName))
            return typeName;

        var typeObj = NewObjectType(typeName, null, []);
        types[typeName] = typeObj;

        var fields = new JsonArray();
        foreach (var (jsonName, node) in GraphQLTypeShape.GetFieldMap(clrType, jsonOptions))
            fields.Add(BuildFieldDescriptor(jsonName, node, types, jsonOptions));

        typeObj["fields"] = fields;
        return typeName;
    }

    /// <summary>
    /// Builds a single <c>__Field</c> descriptor for a reflected <see cref="GraphQLFieldNode"/>.
    /// </summary>
    private static JsonObject BuildFieldDescriptor(string jsonName, GraphQLFieldNode node, Dictionary<string, JsonObject> types, JsonSerializerOptions jsonOptions)
    {
        JsonNode typeRef;
        if (node.IsComplex)
        {
            var nestedTypeName = EnsureObjectType(types, node.ElementType ?? node.PropertyType, jsonOptions);

            // Collections are represented as a (nullable) list of non-null items; single complex references are nullable (no NRT reflection is performed, so a conservative nullable
            // default is used rather than risking an incorrectly over-claimed non-null guarantee).
            typeRef = node.ElementType is not null ? ListOf(NonNullOf(NamedRef(nestedTypeName, "OBJECT"))) : NamedRef(nestedTypeName, "OBJECT");
        }
        else
            typeRef = ScalarRef(node.PropertyType);

        return NewField(jsonName, typeRef);
    }

    /// <summary>
    /// Maps a scalar CLR <paramref name="type"/> to its GraphQL scalar type reference, honouring <see cref="Nullable{T}"/>/reference-type nullability.
    /// </summary>
    private static JsonNode ScalarRef(Type type)
    {
        var isNullableValueType = Nullable.GetUnderlyingType(type) is not null;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var isNullable = isNullableValueType || !underlying.IsValueType;

        var name = underlying.IsEnum || typeof(IReferenceData).IsAssignableFrom(underlying)
            ? "String"
            : underlying switch
            {
                Type t when t == typeof(bool) => "Boolean",
                Type t when t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) => "Int",
                Type t when t == typeof(long) || t == typeof(ulong) => LongScalarName,
                Type t when t == typeof(float) || t == typeof(double) || t == typeof(decimal) => "Float",
                _ => "String" // string, char, Guid, Uri, DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly, and anything else unrecognized.
            };

        var reference = NamedRef(name, "SCALAR");
        return isNullable ? reference : NonNullOf(reference);
    }

    /// <summary>
    /// Ensures a <c>SCALAR</c> type is registered for the specified well-known scalar <paramref name="name"/>.
    /// </summary>
    private static void EnsureScalar(Dictionary<string, JsonObject> types, string name)
    {
        if (types.ContainsKey(name))
            return;

        types[name] = new JsonObject
        {
            ["kind"] = "SCALAR",
            ["name"] = name,
            ["description"] = name switch
            {
                JsonScalarName => "An arbitrary JSON value (object, array, string, number, boolean or null); used for the dynamic 'where'/'orderBy' query root arguments.",
                LongScalarName => "A signed 64-bit integer.",
                _ => null
            },
            ["fields"] = null,
            ["inputFields"] = null,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null,
            ["ofType"] = null
        };
    }

    /// <summary>
    /// Ensures the fixed Relay Cursor Connection <c>PageInfo</c> object type is registered.
    /// </summary>
    private static void EnsurePageInfoType(Dictionary<string, JsonObject> types)
    {
        if (types.ContainsKey(PageInfoTypeName))
            return;

        types[PageInfoTypeName] = NewObjectType(PageInfoTypeName, "Relay Cursor Connection page metadata.", new JsonArray
        {
            NewField("hasNextPage", NonNullOf(NamedRef("Boolean", "SCALAR"))),
            NewField("hasPreviousPage", NonNullOf(NamedRef("Boolean", "SCALAR"))),
            NewField("startCursor", NamedRef("String", "SCALAR")),
            NewField("endCursor", NamedRef("String", "SCALAR"))
        });
    }

    /// <summary>
    /// Creates a new <c>__Type</c> descriptor for an <c>OBJECT</c> kind.
    /// </summary>
    private static JsonObject NewObjectType(string name, string? description, JsonArray fields) => new()
    {
        ["kind"] = "OBJECT",
        ["name"] = name,
        ["description"] = description,
        ["fields"] = fields,
        ["inputFields"] = null,
        ["interfaces"] = new JsonArray(),
        ["enumValues"] = null,
        ["possibleTypes"] = null,
        ["ofType"] = null
    };

    /// <summary>
    /// Creates a new <c>__Field</c> descriptor.
    /// </summary>
    private static JsonObject NewField(string name, JsonNode typeRef, JsonArray? args = null) => new()
    {
        ["name"] = name,
        ["description"] = null,
        ["args"] = args ?? [],
        ["type"] = typeRef,
        ["isDeprecated"] = false,
        ["deprecationReason"] = null
    };

    /// <summary>
    /// Creates a new <c>__InputValue</c> descriptor (used for field arguments).
    /// </summary>
    private static JsonObject NewArg(string name, JsonNode typeRef) => new() { ["name"] = name, ["description"] = null, ["type"] = typeRef, ["defaultValue"] = null };

    /// <summary>
    /// Creates a terminal named <c>__Type</c> reference (<c>ofType</c> is <see langword="null"/>).
    /// </summary>
    private static JsonObject NamedRef(string name, string kind) => new() { ["kind"] = kind, ["name"] = name, ["ofType"] = null };

    /// <summary>
    /// Wraps a <c>__Type</c> reference as <c>NON_NULL</c>.
    /// </summary>
    private static JsonObject NonNullOf(JsonNode inner) => new() { ["kind"] = "NON_NULL", ["name"] = null, ["ofType"] = inner };

    /// <summary>
    /// Wraps a <c>__Type</c> reference as <c>LIST</c>.
    /// </summary>
    private static JsonObject ListOf(JsonNode inner) => new() { ["kind"] = "LIST", ["name"] = null, ["ofType"] = inner };
}

/// <summary>
/// Represents the built spec-compliant GraphQL introspection schema graph: the <c>__Schema</c> object content (for the <c>__schema</c> meta-field) and a name-keyed lookup of every named
/// type within it (for the <c>__type(name:)</c> meta-field).
/// </summary>
/// <param name="Schema">The <c>__Schema</c> object content.</param>
/// <param name="TypesByName">The name-keyed lookup of every named <c>__Type</c> within <see cref="Schema"/>.</param>
internal sealed record GraphQLIntrospectionDocument(JsonObject Schema, IReadOnlyDictionary<string, JsonObject> TypesByName);
