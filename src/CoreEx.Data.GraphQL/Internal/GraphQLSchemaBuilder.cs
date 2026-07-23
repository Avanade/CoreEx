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
    public static JsonElement Build(GraphQLLiteOptions options, JsonSerializerOptions jsonOptions)
    {
        var roots = new JsonObject();

        foreach (var (name, root) in options.QueryRoots)
        {
            roots[name] = new JsonObject
            {
                ["kind"] = "query",
                ["filterOrderBy"] = JsonNode.Parse(root.QueryArgsConfig.ToJsonSchema().GetRawText()),
                ["fields"] = BuildFieldsShape(root.ItemType, jsonOptions)
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
