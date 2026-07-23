namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Resolves a GraphQL <see cref="GraphQLSelectionSet"/> against a reflected <see cref="GraphQLTypeShape"/> field map, producing the flattened <see cref="JsonFilter"/> include paths and any
/// unknown-field errors.
/// </summary>
/// <remarks>Selection sets may traverse arbitrarily nested <b>complex properties already present on the DTO's own object graph</b> (e.g. <c>address { street city }</c>); they cannot request a
/// field that would require invoking a different registered root/resolver (cross-repository resolution is out of scope for v1). Fragments, inline fragments and directives are not supported in v1
/// and are silently skipped (non-goal).</remarks>
internal static class GraphQLSelectionResolver
{
    /// <summary>
    /// Resolves the specified <paramref name="selectionSet"/> against <paramref name="itemType"/>.
    /// </summary>
    /// <param name="selectionSet">The GraphQL selection set for the root field.</param>
    /// <param name="itemType">The DTO <see cref="Type"/> returned by the root's resolver.</param>
    /// <param name="jsonOptions">The <see cref="JsonSerializerOptions"/> used to determine JSON property names.</param>
    /// <param name="rootFieldName">The root field name (used for error <see cref="GraphQLEngineError.Path"/>).</param>
    /// <returns>The flattened <see cref="JsonFilter"/> include paths and any unknown-field <see cref="GraphQLEngineError"/>s.</returns>
    public static (List<string> Paths, List<GraphQLEngineError> Errors) Resolve(GraphQLSelectionSet? selectionSet, Type itemType, JsonSerializerOptions jsonOptions, string rootFieldName)
    {
        var paths = new List<string>();
        var errors = new List<GraphQLEngineError>();
        Walk(selectionSet, GraphQLTypeShape.GetFieldMap(itemType, jsonOptions), jsonOptions, "$", [rootFieldName], paths, errors);
        return (paths, errors);
    }

    /// <summary>
    /// Recursively walks the specified <paramref name="selectionSet"/> against <paramref name="fieldMap"/>, accumulating flattened <paramref name="paths"/> and unknown-field <paramref name="errors"/>.
    /// </summary>
    private static void Walk(GraphQLSelectionSet? selectionSet, IReadOnlyDictionary<string, GraphQLFieldNode> fieldMap, JsonSerializerOptions jsonOptions, string jsonPathPrefix,
        IReadOnlyList<string> errorPath, List<string> paths, List<GraphQLEngineError> errors)
    {
        if (selectionSet?.Selections is null)
            return;

        foreach (var selection in selectionSet.Selections)
        {
            // Fragments/inline-fragments are not supported in v1 (non-goal) - silently skipped.
            if (selection is not GraphQLField field)
                continue;

            var name = field.Name.StringValue;
            if (!fieldMap.TryGetValue(name, out var node))
            {
                errors.Add(new GraphQLEngineError($"Unknown field '{name}'.") { Path = [.. errorPath, name], Extensions = new Dictionary<string, object?> { ["code"] = "UNKNOWN_FIELD" } });
                continue;
            }

            var path = $"{jsonPathPrefix}.{node.JsonName}";
            if (node.IsComplex && node.Children is not null)
                Walk(field.SelectionSet, node.Children.Value, jsonOptions, path, [.. errorPath, name], paths, errors);
            else
                paths.Add(path);
        }
    }
}
