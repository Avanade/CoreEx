namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Builds the GraphQL-lite schema/discovery document for a set of registered roots, composing the existing <see cref="QueryArgsConfig.ToJsonSchema"/> output with a reflection-derived
/// description of each root's selectable output fields.
/// </summary>
internal static class GraphQLSchemaBuilder
{
    /// <summary>
    /// Builds the schema document for the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The <see cref="GraphQLLiteOptions"/> describing the registered roots.</param>
    /// <param name="jsonOptions">The <see cref="JsonSerializerOptions"/> used to derive each root's JSON field names.</param>
    /// <returns>The schema/discovery document.</returns>
    public static JsonElement Build(GraphQLLiteOptions options, JsonSerializerOptions jsonOptions)
    {
        var roots = new JsonObject();

        foreach (var (name, root) in options.QueryRoots)
        {
            var filterOrderBySchema = root.QueryArgsConfig.ToJsonSchema();
            roots[name] = new JsonObject
            {
                ["kind"] = "query",
                ["where"] = filterOrderBySchema.TryGetProperty("filter", out var filterSchema) ? JsonNode.Parse(filterSchema.GetRawText()) : null,
                ["orderBy"] = filterOrderBySchema.TryGetProperty("orderby", out var orderBySchema) ? JsonNode.Parse(orderBySchema.GetRawText()) : null,
                ["fields"] = BuildConnectionFieldsShape(root.ItemType, jsonOptions)
            };
        }

        foreach (var (name, root) in options.ItemRoots)
        {
            roots[name] = new JsonObject
            {
                ["kind"] = "get",
                ["fields"] = BuildFieldsShape(root.ItemType, jsonOptions)
            };
        }

        var doc = new JsonObject { ["roots"] = roots };
        return JsonSerializer.SerializeToElement(doc);
    }

    /// <summary>
    /// Builds the fixed Relay Cursor Connection <c>edges</c>/<c>pageInfo</c>/<c>totalCount</c> shape for a query root, wrapping the reflection-derived <paramref name="itemType"/> shape
    /// under <c>edges.items.node</c>.
    /// </summary>
    private static JsonObject BuildConnectionFieldsShape(Type itemType, JsonSerializerOptions jsonOptions) => new()
    {
        ["edges"] = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonObject { ["node"] = BuildFieldsShape(itemType, jsonOptions), ["cursor"] = "String" }
        },
        ["pageInfo"] = new JsonObject
        {
            ["hasNextPage"] = "Boolean",
            ["hasPreviousPage"] = "Boolean",
            ["startCursor"] = "String",
            ["endCursor"] = "String"
        },
        ["totalCount"] = "Int64"
    };

    /// <summary>
    /// Builds the reflection-derived selectable-fields shape for the specified <paramref name="type"/>.
    /// </summary>
    private static JsonObject BuildFieldsShape(Type type, JsonSerializerOptions jsonOptions)
    {
        var obj = new JsonObject();
        foreach (var (jsonName, node) in GraphQLTypeShape.GetFieldMap(type, jsonOptions))
        {
            if (node.IsComplex && node.Children is not null)
            {
                var nested = BuildFieldsShape(node.ElementType ?? node.PropertyType, jsonOptions);
                obj[jsonName] = node.ElementType is not null ? new JsonObject { ["type"] = "array", ["items"] = nested } : nested;
            }
            else
                obj[jsonName] = (Nullable.GetUnderlyingType(node.PropertyType) ?? node.PropertyType).Name;
        }

        return obj;
    }
}
