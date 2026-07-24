namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Resolves a GraphQL <see cref="GraphQLSelectionSet"/> against a reflected <see cref="GraphQLTypeShape"/> field map, producing the flattened <see cref="JsonFilter"/> include paths and any
/// unknown-field errors.
/// </summary>
/// <remarks>Selection sets may traverse arbitrarily nested <b>complex properties already present on the DTO's own object graph</b> (e.g. <c>address { street city }</c>); they cannot request a
/// field that would require invoking a different registered root/resolver (cross-repository resolution is out of scope for v1). Fragments, inline fragments and directives are not supported in v1
/// and produce a <see cref="GraphQLEngineError"/> rather than being silently skipped. The <c>__typename</c> meta-field is always accepted (see <see cref="GraphQLResponseShaper"/> for how it is
/// populated) since standard GraphQL clients (e.g. Apollo Client, Relay) auto-inject it into every selection set.</remarks>
internal static class GraphQLSelectionResolver
{
    /// <summary>
    /// The reserved GraphQL meta-field name that resolves to the current object's type name.
    /// </summary>
    public const string TypeNameField = "__typename";

    /// <summary>
    /// Resolves the specified <paramref name="selectionSet"/> against <paramref name="itemType"/>.
    /// </summary>
    /// <param name="selectionSet">The GraphQL selection set for the root field.</param>
    /// <param name="itemType">The DTO <see cref="Type"/> returned by the root's resolver.</param>
    /// <param name="jsonOptions">The <see cref="JsonSerializerOptions"/> used to determine JSON property names.</param>
    /// <param name="rootFieldName">The root field name (used in error messages).</param>
    /// <param name="errorPath">The <see cref="GraphQLEngineError.Path"/> prefix; defaults to <c>[<paramref name="rootFieldName"/>]</c> where not specified (e.g. an item root). A query root's
    /// nested <c>node</c> selection passes the full <c>[alias, "edges", "node"]</c> path here so errors point at the correct nesting.</param>
    /// <returns>The flattened <see cref="JsonFilter"/> include paths and any unknown-field <see cref="GraphQLEngineError"/>s.</returns>
    public static (List<string> Paths, List<GraphQLEngineError> Errors) Resolve(GraphQLSelectionSet? selectionSet, Type itemType, JsonSerializerOptions jsonOptions, string rootFieldName,
        IReadOnlyList<string>? errorPath = null)
    {
        var paths = new List<string>();
        var errors = new List<GraphQLEngineError>();
        errorPath ??= [rootFieldName];

        if (selectionSet?.Selections is not { Count: > 0 })
        {
            errors.Add(SelectionRequiredError(rootFieldName, errorPath));
            return (paths, errors);
        }

        Walk(selectionSet, GraphQLTypeShape.GetFieldMap(itemType, jsonOptions), jsonOptions, "$", errorPath, paths, errors);
        return (paths, errors);
    }

    /// <summary>
    /// Recursively walks the specified <paramref name="selectionSet"/> against <paramref name="fieldMap"/>, accumulating flattened <paramref name="paths"/> and unknown-field/fragment <paramref name="errors"/>.
    /// </summary>
    private static void Walk(GraphQLSelectionSet? selectionSet, IReadOnlyDictionary<string, GraphQLFieldNode> fieldMap, JsonSerializerOptions jsonOptions, string jsonPathPrefix,
        IReadOnlyList<string> errorPath, List<string> paths, List<GraphQLEngineError> errors)
    {
        if (selectionSet?.Selections is null)
            return;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not GraphQLField field)
            {
                errors.Add(new GraphQLEngineError("Fragment spreads and inline fragments are not supported.") { Path = [.. errorPath], Extensions = new Dictionary<string, object?> { ["code"] = "FRAGMENTS_NOT_SUPPORTED" } });
                continue;
            }

            var name = field.Name.StringValue;
            if (name == TypeNameField)
                continue; // No underlying property to project; populated separately by GraphQLResponseShaper.

            if (!fieldMap.TryGetValue(name, out var node))
            {
                errors.Add(new GraphQLEngineError($"Unknown field '{name}'.") { Path = [.. errorPath, name], Extensions = new Dictionary<string, object?> { ["code"] = "UNKNOWN_FIELD" } });
                continue;
            }

            var path = $"{jsonPathPrefix}.{node.JsonName}";
            if (node.IsComplex && node.Children is not null)
            {
                if (field.SelectionSet?.Selections is not { Count: > 0 })
                {
                    errors.Add(SelectionRequiredError(name, [.. errorPath, name]));
                    continue;
                }

                Walk(field.SelectionSet, node.Children.Value, jsonOptions, path, [.. errorPath, name], paths, errors);
            }
            else
                paths.Add(path);
        }
    }

    /// <summary>
    /// Creates the <c>SELECTION_REQUIRED</c> <see cref="GraphQLEngineError"/> raised where an object-typed field (or an item/node root) is selected without a sub-selection set.
    /// </summary>
    private static GraphQLEngineError SelectionRequiredError(string fieldName, IReadOnlyList<string> path) =>
        new($"Field '{fieldName}' must have a selection of subfields.") { Path = [.. path], Extensions = new Dictionary<string, object?> { ["code"] = "SELECTION_REQUIRED" } };
}
