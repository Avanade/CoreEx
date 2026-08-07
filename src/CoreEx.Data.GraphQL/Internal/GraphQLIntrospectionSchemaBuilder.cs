namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Builds a spec-compliant GraphQL introspection schema graph (see the <see href="https://spec.graphql.org/October2021/#sec-Introspection">GraphQL introspection specification</see>) for the
/// roots registered on a <see cref="GraphQLLiteOptions"/>, exposed via the <c>__schema</c>/<c>__type</c> meta-fields.
/// </summary>
/// <remarks>The graph is built once (see <see cref="GraphQLEngine"/>'s lazy caching) rather than per-request: it is derived purely from registration-time information (root names, CLR item
/// types, and each query root's item shape, plus each list root's <see cref="QueryArgsConfig"/>), which does not change for the lifetime of a registered <see cref="GraphQLLiteOptions"/>
/// instance.
/// <para>Where a query root's <see cref="QueryArgsConfig"/> has configured filter/order-by fields, its <c>where</c>/<c>orderBy</c> arguments are described as fully-typed
/// <c>&lt;Item&gt;WhereInput</c>/<c>&lt;Item&gt;OrderByInput</c> graphs (see <see cref="BuildWhereInputType"/>/<see cref="BuildOrderByInputType"/>), derived from the same public
/// <see cref="QueryArgsConfig.ToJsonSchema"/> already used to power the legacy REST <c>$filter</c>/<c>$orderby</c> schema description - <b>no</b> new configuration is required. Where no
/// fields are configured, the argument is omitted entirely rather than advertising an unusable shape. The opaque <c>JSON</c> custom scalar remains defined (for any future arbitrary-JSON
/// property use) but is no longer used for <c>where</c>/<c>orderBy</c>.</para>
/// <para>Known simplifications (documented in <c>AGENTS.md</c>/<c>README.md</c>): every field of a given JSON-schema type (<c>string</c>/<c>integer</c>/<c>number</c>/<c>boolean</c>) shares
/// one generic <c>&lt;Type&gt;FilterInput</c> operator set (e.g. <see cref="StringFilterInputName"/>) rather than a per-field-restricted shape, so a field may advertise an operator
/// (e.g. <c>gt</c>) that its specific <see cref="QueryFilterParser"/> configuration does not actually permit - <see cref="QueryFilterParser"/> still enforces the real legality at execution
/// time (defense in depth, matching the REST <c>$filter</c> behaviour); <c>&lt;Item&gt;WhereInput</c>/<c>&lt;Item&gt;OrderByInput</c> field names are the all-lowercase names already
/// reported by <see cref="QueryArgsConfig.ToJsonSchema"/> (e.g. <c>subcategory</c> rather than <c>subCategory</c>) rather than the DTO's camelCase JSON naming, which has no effect on
/// correctness since field name matching is case-insensitive; CLR <see langword="enum"/> and <see cref="IReferenceData"/> <i>output</i> properties are still described as the <c>String</c>
/// scalar (matching their actual JSON wire representation) rather than a spec <c>ENUM</c> type; and a single-item <c>AddGet</c> root only advertises an <c>id: ID!</c> argument where its
/// registered item <see cref="Type"/> implements <see cref="IReadOnlyIdentifier{TId}"/> - otherwise it advertises no <c>id</c> argument at all, rather than guessing at an argument shape
/// the registration API does not declare - but always advertises <c>includeText</c>/<c>includeInactive</c> alongside it, since <see cref="GraphQLEngine"/> honours both for item roots
/// regardless of the item type's identifier shape.</para></remarks>
internal static class GraphQLIntrospectionSchemaBuilder
{
    private const string QueryTypeName = "Query";
    private const string PageInfoTypeName = "PageInfo";
    private const string JsonScalarName = "JSON";
    private const string LongScalarName = "Long";
    private const string SortDirectionEnumName = "SortDirection";
    private const string StringFilterInputName = "StringFilterInput";
    private const string IntFilterInputName = "IntFilterInput";
    private const string LongFilterInputName = "LongFilterInput";
    private const string FloatFilterInputName = "FloatFilterInput";
    private const string BooleanFilterInputName = "BooleanFilterInput";
    private const string NullFilterInputName = "NullFilterInput";

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
        var typeOwners = new Dictionary<string, Type>();

        foreach (var scalar in _builtInScalars)
            EnsureScalar(types, scalar);

        EnsureScalar(types, JsonScalarName);
        EnsureScalar(types, LongScalarName);
        EnsurePageInfoType(types);

        var queryFields = new JsonArray();

        foreach (var root in options.QueryRoots.Values)
            queryFields.Add(BuildQueryRootField(root, types, typeOwners, jsonOptions));

        foreach (var root in options.ItemRoots.Values)
            queryFields.Add(BuildItemRootField(root, types, typeOwners, jsonOptions));

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
    private static JsonObject BuildQueryRootField(GraphQLQueryRoot root, Dictionary<string, JsonObject> types, Dictionary<string, Type> typeOwners, JsonSerializerOptions jsonOptions)
    {
        var itemTypeName = EnsureObjectType(types, typeOwners, root.ItemType, jsonOptions);
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
            NewArg("after", NamedRef("String", "SCALAR"))
        };

        var whereTypeName = BuildWhereInputType(types, itemTypeName, root.QueryArgsConfig);
        if (whereTypeName is not null)
            args.Add(NewArg("where", NamedRef(whereTypeName, "INPUT_OBJECT")));

        var orderByTypeName = BuildOrderByInputType(types, itemTypeName, root.QueryArgsConfig);
        if (orderByTypeName is not null)
            args.Add(NewArg("orderBy", ListOf(NonNullOf(NamedRef(orderByTypeName, "INPUT_OBJECT")))));

        args.Add(NewArg("includeText", NamedRef("Boolean", "SCALAR")));
        args.Add(NewArg("includeInactive", NamedRef("Boolean", "SCALAR")));

        return NewField(root.Name, NonNullOf(NamedRef(connectionTypeName, "OBJECT")), args);
    }

    /// <summary>
    /// Builds the <c>Query</c> type field descriptor for a registered <see cref="GraphQLItemRoot"/>, advertising an <c>id: ID!</c> argument only where its item <see cref="Type"/>
    /// implements <see cref="IReadOnlyIdentifier{TId}"/>, plus the <c>includeText</c>/<c>includeInactive</c> arguments for consistency with list query roots.
    /// </summary>
    /// <remarks><c>includeText</c> is honoured for item roots (see <see cref="GraphQLArgsMapper.ApplyItemRootFlags"/>); <c>includeInactive</c> is advertised but has no effect on a
    /// single-item get (there is no filter to apply it to) - it is only meaningful on list query roots.</remarks>
    private static JsonObject BuildItemRootField(GraphQLItemRoot root, Dictionary<string, JsonObject> types, Dictionary<string, Type> typeOwners, JsonSerializerOptions jsonOptions)
    {
        var itemTypeName = EnsureObjectType(types, typeOwners, root.ItemType, jsonOptions);
        var args = new JsonArray();

        if (root.ItemType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyIdentifier<>)))
            args.Add(NewArg("id", NonNullOf(NamedRef("ID", "SCALAR"))));

        args.Add(NewArg("includeText", NamedRef("Boolean", "SCALAR")));
        args.Add(NewArg("includeInactive", NamedRef("Boolean", "SCALAR")));

        return NewField(root.Name, NamedRef(itemTypeName, "OBJECT"), args);
    }

    /// <summary>
    /// Builds the <c>&lt;Item&gt;WhereInput</c> <c>INPUT_OBJECT</c> type for a query root's <see cref="QueryArgsConfig"/>, derived from its public
    /// <see cref="QueryArgsConfig.ToJsonSchema"/> filter field descriptions.
    /// </summary>
    /// <param name="types">The name-keyed type registry.</param>
    /// <param name="itemTypeName">The item type's GraphQL name (used as the <c>&lt;Item&gt;</c> prefix).</param>
    /// <param name="config">The query root's <see cref="QueryArgsConfig"/>.</param>
    /// <returns>The <c>&lt;Item&gt;WhereInput</c> type name, or <see langword="null"/> where no filter fields are configured (the argument is omitted rather than advertising an
    /// unusable empty shape).</returns>
    private static string? BuildWhereInputType(Dictionary<string, JsonObject> types, string itemTypeName, QueryArgsConfig config)
    {
        if (!config.HasFilterParser || !config.FilterParser.HasFields)
            return null;

        var typeName = $"{itemTypeName}WhereInput";
        if (types.ContainsKey(typeName))
            return typeName;

        EnsureFilterInputTypes(types);

        var inputFields = new JsonArray
        {
            NewArg("and", ListOf(NonNullOf(NamedRef(typeName, "INPUT_OBJECT")))),
            NewArg("or", ListOf(NonNullOf(NamedRef(typeName, "INPUT_OBJECT")))),
            NewArg("not", NamedRef(typeName, "INPUT_OBJECT"))
        };

        foreach (var field in config.FilterParser.ToJsonSchema().GetProperty("fields").EnumerateObject())
        {
            var schemaType = field.Value.GetProperty("type").GetString();
            var format = field.Value.TryGetProperty("format", out var formatValue) ? formatValue.GetString() : null;
            inputFields.Add(NewArg(field.Name, NamedRef(FilterInputTypeNameFor(schemaType, format), "INPUT_OBJECT")));
        }

        types[typeName] = NewInputObjectType(typeName, $"Filter criteria for '{itemTypeName}'.", inputFields);
        return typeName;
    }

    /// <summary>
    /// Builds the <c>&lt;Item&gt;OrderByInput</c> <c>INPUT_OBJECT</c> type for a query root's <see cref="QueryArgsConfig"/>, derived from its public
    /// <see cref="QueryArgsConfig.ToJsonSchema"/> order-by field descriptions.
    /// </summary>
    /// <param name="types">The name-keyed type registry.</param>
    /// <param name="itemTypeName">The item type's GraphQL name (used as the <c>&lt;Item&gt;</c> prefix).</param>
    /// <param name="config">The query root's <see cref="QueryArgsConfig"/>.</param>
    /// <returns>The <c>&lt;Item&gt;OrderByInput</c> type name, or <see langword="null"/> where no order-by fields are configured (the argument is omitted rather than advertising an
    /// unusable empty shape).</returns>
    private static string? BuildOrderByInputType(Dictionary<string, JsonObject> types, string itemTypeName, QueryArgsConfig config)
    {
        if (!config.HasOrderByParser || !config.OrderByParser.HasFields)
            return null;

        var typeName = $"{itemTypeName}OrderByInput";
        if (types.ContainsKey(typeName))
            return typeName;

        EnsureSortDirectionEnum(types);

        var inputFields = new JsonArray();
        foreach (var field in config.OrderByParser.ToJsonSchema().GetProperty("fields").EnumerateObject())
            inputFields.Add(NewArg(field.Name, NamedRef(SortDirectionEnumName, "ENUM")));

        types[typeName] = NewInputObjectType(typeName, $"Sort criteria for '{itemTypeName}'.", inputFields);
        return typeName;
    }

    /// <summary>
    /// Ensures the shared, generic per-scalar-kind filter operator <c>INPUT_OBJECT</c> types are registered (reused across every <c>&lt;Item&gt;WhereInput</c>).
    /// </summary>
    private static void EnsureFilterInputTypes(Dictionary<string, JsonObject> types)
    {
        if (types.ContainsKey(StringFilterInputName))
            return;

        types[StringFilterInputName] = NewInputObjectType(StringFilterInputName, "String field filter operators.", new JsonArray
        {
            NewArg("eq", NamedRef("String", "SCALAR")),
            NewArg("ne", NamedRef("String", "SCALAR")),
            NewArg("in", ListOf(NonNullOf(NamedRef("String", "SCALAR")))),
            NewArg("startsWith", NamedRef("String", "SCALAR")),
            NewArg("endsWith", NamedRef("String", "SCALAR")),
            NewArg("contains", NamedRef("String", "SCALAR"))
        });

        types[IntFilterInputName] = NewInputObjectType(IntFilterInputName, "Int field filter operators.", NewComparableFilterFields("Int"));
        types[LongFilterInputName] = NewInputObjectType(LongFilterInputName, "Long field filter operators.", NewComparableFilterFields(LongScalarName));
        types[FloatFilterInputName] = NewInputObjectType(FloatFilterInputName, "Float field filter operators.", NewComparableFilterFields("Float"));

        types[BooleanFilterInputName] = NewInputObjectType(BooleanFilterInputName, "Boolean field filter operators.", new JsonArray
        {
            NewArg("eq", NamedRef("Boolean", "SCALAR")),
            NewArg("ne", NamedRef("Boolean", "SCALAR"))
        });

        types[NullFilterInputName] = NewInputObjectType(NullFilterInputName,
            "Filter operators for a null-only comparison field; only the literal 'null' value is meaningful (any other value fails at execution time).", new JsonArray
        {
            NewArg("eq", NamedRef("Boolean", "SCALAR")),
            NewArg("ne", NamedRef("Boolean", "SCALAR"))
        });
    }

    /// <summary>
    /// Builds the shared <c>eq</c>/<c>ne</c>/<c>gt</c>/<c>ge</c>/<c>lt</c>/<c>le</c>/<c>in</c> comparison operator fields for a scalar-typed filter <c>INPUT_OBJECT</c>.
    /// </summary>
    /// <param name="scalarName">The GraphQL scalar name (e.g. <c>Int</c>) shared by every operator field.</param>
    private static JsonArray NewComparableFilterFields(string scalarName) =>
    [
        NewArg("eq", NamedRef(scalarName, "SCALAR")),
        NewArg("ne", NamedRef(scalarName, "SCALAR")),
        NewArg("gt", NamedRef(scalarName, "SCALAR")),
        NewArg("ge", NamedRef(scalarName, "SCALAR")),
        NewArg("lt", NamedRef(scalarName, "SCALAR")),
        NewArg("le", NamedRef(scalarName, "SCALAR")),
        NewArg("in", ListOf(NonNullOf(NamedRef(scalarName, "SCALAR"))))
    ];

    /// <summary>
    /// Maps a <see cref="QueryArgsConfig.ToJsonSchema"/> filter field's reported JSON-schema <paramref name="schemaType"/>/<paramref name="format"/> to the matching shared
    /// filter <c>INPUT_OBJECT</c> type name.
    /// </summary>
    /// <param name="schemaType">The reported JSON-schema type (<c>string</c>/<c>integer</c>/<c>number</c>/<c>boolean</c>/<c>object</c>).</param>
    /// <param name="format">The reported JSON-schema format (e.g. <c>int64</c>/<c>uint64</c> distinguishing a 64-bit integer field), if any.</param>
    private static string FilterInputTypeNameFor(string? schemaType, string? format) => schemaType switch
    {
        "integer" => format is "int64" or "uint64" ? LongFilterInputName : IntFilterInputName,
        "number" => FloatFilterInputName,
        "boolean" => BooleanFilterInputName,
        "object" => NullFilterInputName,
        _ => StringFilterInputName
    };

    /// <summary>
    /// Ensures the shared <c>SortDirection</c> <c>ENUM</c> type (<c>ASC</c>/<c>DESC</c>) is registered.
    /// </summary>
    private static void EnsureSortDirectionEnum(Dictionary<string, JsonObject> types)
    {
        if (types.ContainsKey(SortDirectionEnumName))
            return;

        types[SortDirectionEnumName] = new JsonObject
        {
            ["kind"] = "ENUM",
            ["name"] = SortDirectionEnumName,
            ["description"] = "A sort direction.",
            ["fields"] = null,
            ["inputFields"] = null,
            ["interfaces"] = null,
            ["enumValues"] = new JsonArray { NewEnumValue("ASC"), NewEnumValue("DESC") },
            ["possibleTypes"] = null,
            ["ofType"] = null
        };
    }

    /// <summary>
    /// Creates a new <c>__EnumValue</c> descriptor.
    /// </summary>
    private static JsonObject NewEnumValue(string name) => new() { ["name"] = name, ["description"] = null, ["isDeprecated"] = false, ["deprecationReason"] = null };

    /// <summary>
    /// Ensures an <c>OBJECT</c> type is registered for the specified <paramref name="clrType"/>, recursing into its <see cref="GraphQLTypeShape"/>-derived complex fields.
    /// </summary>
    /// <remarks>A placeholder is registered <i>before</i> recursing into fields, guarding against infinite recursion for self-referencing (cyclic) DTO graphs.
    /// <para>The <paramref name="depth"/> parameter mirrors <see cref="GraphQLTypeShape"/>'s own field-map recursion depth (root = <c>0</c>, incrementing by one per nesting hop) so that a
    /// type nested at or beyond <see cref="GraphQLTypeShape.MaxDepth"/> is registered with an empty <c>fields</c> list here too, rather than advertising fields the runtime would reject as
    /// <c>UNKNOWN_FIELD</c> once <see cref="GraphQLTypeShape.GetFieldMap"/>'s equivalent depth cap kicks in. This is only an approximation for a shared/self-referential CLR type reachable at
    /// different depths from different roots: the <paramref name="types"/> registry is keyed by type name and built once, so whichever depth first encounters a given type determines whether
    /// its fields are populated for <i>every</i> path that reaches it.</para></remarks>
    private static string EnsureObjectType(Dictionary<string, JsonObject> types, Dictionary<string, Type> typeOwners, Type clrType, JsonSerializerOptions jsonOptions, int depth = 0)
    {
        var typeName = clrType.Name;
        if (typeOwners.TryGetValue(typeName, out var owner))
        {
            if (owner != clrType)
                throw new InvalidOperationException(
                    $"GraphQL type name '{typeName}' is already registered for CLR type '{owner.FullName}'; it cannot also be used for '{clrType.FullName}'. Registered root/item types must not share a simple type name across different namespaces.");

            return typeName;
        }

        typeOwners[typeName] = clrType;
        var typeObj = NewObjectType(typeName, null, []);
        types[typeName] = typeObj;

        var fields = new JsonArray();
        if (depth < GraphQLTypeShape.MaxDepth)
        {
            foreach (var (jsonName, node) in GraphQLTypeShape.GetFieldMap(clrType, jsonOptions))
                fields.Add(BuildFieldDescriptor(jsonName, node, types, typeOwners, jsonOptions, depth));
        }

        typeObj["fields"] = fields;
        return typeName;
    }

    /// <summary>
    /// Builds a single <c>__Field</c> descriptor for a reflected <see cref="GraphQLFieldNode"/>.
    /// </summary>
    private static JsonObject BuildFieldDescriptor(string jsonName, GraphQLFieldNode node, Dictionary<string, JsonObject> types, Dictionary<string, Type> typeOwners, JsonSerializerOptions jsonOptions, int depth)
    {
        JsonNode typeRef;
        if (node.IsComplex)
        {
            // Non-collection complex properties (ElementType is null) must be unwrapped from Nullable<T> before use - both to avoid registering a bogus "Nullable`1" OBJECT type,
            // and so EnsureObjectType/GetFieldMap recurse into the underlying struct's own properties rather than Nullable<T>'s HasValue/Value.
            var childType = node.ElementType ?? Nullable.GetUnderlyingType(node.PropertyType) ?? node.PropertyType;
            var nestedTypeName = EnsureObjectType(types, typeOwners, childType, jsonOptions, depth + 1);

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
    /// Creates a new <c>__Type</c> descriptor for an <c>INPUT_OBJECT</c> kind.
    /// </summary>
    private static JsonObject NewInputObjectType(string name, string? description, JsonArray inputFields) => new()
    {
        ["kind"] = "INPUT_OBJECT",
        ["name"] = name,
        ["description"] = description,
        ["fields"] = null,
        ["inputFields"] = inputFields,
        ["interfaces"] = null,
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
