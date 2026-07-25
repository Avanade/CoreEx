namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Resolves a query root's GraphQL selection set against the fixed, engine-defined Relay <see href="https://relay.dev/graphql/connections.htm">Cursor Connections</see>
/// <c>Connection</c>/<c>Edge</c>/<c>PageInfo</c> shape, producing the validated field aliases together with the underlying item type's own <c>node</c> selection set (which is
/// resolved separately, unchanged, via <see cref="GraphQLSelectionResolver"/>).
/// </summary>
/// <remarks>Unlike <see cref="GraphQLTypeShape"/>'s DTO-reflected field maps, the <c>Connection</c>/<c>Edge</c>/<c>PageInfo</c> wrapper fields are synthetic — they do not exist
/// on any real CLR type returned by a root resolver — so this resolver validates against a small, fixed field set rather than reflection. A query root's selection set must
/// select from <c>edges</c>, <c>pageInfo</c> and/or <c>totalCount</c> (plus <c>__typename</c>); it can no longer select the underlying item's own fields directly at the root -
/// those are only reachable via <c>edges.node</c>.</remarks>
internal static class GraphQLConnectionResolver
{
    /// <summary>
    /// The <c>edges</c> Connection field name.
    /// </summary>
    public const string EdgesField = "edges";

    /// <summary>
    /// The <c>node</c> Edge field name.
    /// </summary>
    public const string NodeField = "node";

    /// <summary>
    /// The <c>cursor</c> Edge field name.
    /// </summary>
    public const string CursorField = "cursor";

    /// <summary>
    /// The <c>pageInfo</c> Connection field name.
    /// </summary>
    public const string PageInfoField = "pageInfo";

    /// <summary>
    /// The <c>totalCount</c> Connection field name.
    /// </summary>
    public const string TotalCountField = "totalCount";

    /// <summary>
    /// The <c>hasNextPage</c> PageInfo field name.
    /// </summary>
    public const string HasNextPageField = "hasNextPage";

    /// <summary>
    /// The <c>hasPreviousPage</c> PageInfo field name.
    /// </summary>
    public const string HasPreviousPageField = "hasPreviousPage";

    /// <summary>
    /// The <c>startCursor</c> PageInfo field name.
    /// </summary>
    public const string StartCursorField = "startCursor";

    /// <summary>
    /// The <c>endCursor</c> PageInfo field name.
    /// </summary>
    public const string EndCursorField = "endCursor";

    /// <summary>
    /// Resolves the specified query root <paramref name="selectionSet"/> against the Connection shape.
    /// </summary>
    /// <param name="selectionSet">The query root field's GraphQL selection set.</param>
    /// <param name="errorPath">The root field's error <see cref="GraphQLEngineError.Path"/> prefix.</param>
    /// <returns>The resolved <see cref="ConnectionSelection"/> (or <see langword="null"/> where <paramref name="selectionSet"/> is invalid) and any errors.</returns>
    public static (ConnectionSelection? Selection, List<GraphQLEngineError> Errors) Resolve(GraphQLSelectionSet? selectionSet, IReadOnlyList<string> errorPath)
    {
        var errors = new List<GraphQLEngineError>();
        if (!TryGetSelections(selectionSet, errorPath, errors, out var selections))
            return (null, errors);

        string? connectionTypeNameAlias = null, edgesAlias = null, pageInfoAlias = null, totalCountAlias = null;
        string? nodeAlias = null, edgeTypeNameAlias = null, cursorAlias = null;
        GraphQLSelectionSet? nodeSelectionSet = null;
        var pageInfoFieldAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? pageInfoTypeNameAlias = null;

        foreach (var field in selections)
        {
            var name = field.Name.StringValue;
            var alias = field.Alias?.Name.StringValue ?? name;

            if (name == GraphQLSelectionResolver.TypeNameField)
                connectionTypeNameAlias = alias;
            else if (string.Equals(name, EdgesField, StringComparison.OrdinalIgnoreCase))
            {
                edgesAlias = alias;
                if (TryGetSelections(field.SelectionSet, [.. errorPath, alias], errors, out var edgeSelections))
                {
                    foreach (var edgeField in edgeSelections)
                    {
                        var edgeName = edgeField.Name.StringValue;
                        var edgeAlias = edgeField.Alias?.Name.StringValue ?? edgeName;

                        if (edgeName == GraphQLSelectionResolver.TypeNameField)
                            edgeTypeNameAlias = edgeAlias;
                        else if (string.Equals(edgeName, NodeField, StringComparison.OrdinalIgnoreCase))
                        {
                            nodeAlias = edgeAlias;
                            nodeSelectionSet = edgeField.SelectionSet;
                        }
                        else if (string.Equals(edgeName, CursorField, StringComparison.OrdinalIgnoreCase))
                            cursorAlias = edgeAlias;
                        else
                            errors.Add(NewError($"Unknown field '{edgeName}'; an edge must select from 'node', 'cursor'.", [.. errorPath, alias, edgeAlias]));
                    }
                }
            }
            else if (string.Equals(name, PageInfoField, StringComparison.OrdinalIgnoreCase))
            {
                pageInfoAlias = alias;
                if (TryGetSelections(field.SelectionSet, [.. errorPath, alias], errors, out var pageInfoSelections))
                {
                    foreach (var pageInfoField in pageInfoSelections)
                    {
                        var pageInfoFieldName = pageInfoField.Name.StringValue;
                        var pageInfoFieldAlias = pageInfoField.Alias?.Name.StringValue ?? pageInfoFieldName;

                        if (pageInfoFieldName == GraphQLSelectionResolver.TypeNameField)
                            pageInfoTypeNameAlias = pageInfoFieldAlias;
                        else if (pageInfoFieldName is HasNextPageField or HasPreviousPageField or StartCursorField or EndCursorField)
                            pageInfoFieldAliases[pageInfoFieldName] = pageInfoFieldAlias;
                        else
                            errors.Add(NewError($"Unknown field '{pageInfoFieldName}'; 'pageInfo' must select from 'hasNextPage', 'hasPreviousPage', 'startCursor', 'endCursor'.", [.. errorPath, alias, pageInfoFieldAlias]));
                    }
                }
            }
            else if (string.Equals(name, TotalCountField, StringComparison.OrdinalIgnoreCase))
                totalCountAlias = alias;
            else
                errors.Add(NewError($"Unknown field '{name}'; a query root must select from 'edges', 'pageInfo', 'totalCount' (Relay Cursor Connections).", [.. errorPath, alias]));
        }

        if (errors.Count > 0)
            return (null, errors);

        return (new ConnectionSelection(connectionTypeNameAlias, edgesAlias, edgeTypeNameAlias, nodeAlias, nodeSelectionSet, cursorAlias, pageInfoAlias, pageInfoTypeNameAlias,
            pageInfoFieldAliases, totalCountAlias), errors);
    }

    /// <summary>
    /// Extracts the plain <see cref="GraphQLField"/> selections from <paramref name="selectionSet"/>, adding a <c>SELECTION_REQUIRED</c>/<c>FRAGMENTS_NOT_SUPPORTED</c> error
    /// (and returning <see langword="false"/>) where the selection set is missing or contains a fragment.
    /// </summary>
    /// <param name="selectionSet">The GraphQL selection set to inspect.</param>
    /// <param name="errorPath">The error <see cref="GraphQLEngineError.Path"/> prefix for this level.</param>
    /// <param name="errors">The accumulating error list.</param>
    /// <param name="fields">The extracted plain field selections.</param>
    /// <returns><see langword="true"/> where a selection set was present and contained only plain fields; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetSelections(GraphQLSelectionSet? selectionSet, IReadOnlyList<string> errorPath, List<GraphQLEngineError> errors, out List<GraphQLField> fields)
    {
        fields = [];
        if (selectionSet?.Selections is null)
        {
            errors.Add(NewError("A selection set is required.", errorPath, "SELECTION_REQUIRED"));
            return false;
        }

        var ok = true;
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field)
                fields.Add(field);
            else
            {
                errors.Add(NewError("Fragment spreads and inline fragments are not supported.", errorPath, "FRAGMENTS_NOT_SUPPORTED"));
                ok = false;
            }
        }

        return ok;
    }

    /// <summary>
    /// Creates a new <see cref="GraphQLEngineError"/> with the specified message, path and error code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="path">The error <see cref="GraphQLEngineError.Path"/>.</param>
    /// <param name="code">The error code (defaults to <c>UNKNOWN_FIELD</c>).</param>
    /// <returns>The <see cref="GraphQLEngineError"/>.</returns>
    private static GraphQLEngineError NewError(string message, IReadOnlyList<string> path, string code = "UNKNOWN_FIELD") =>
        new(message) { Path = [.. path], Extensions = new Dictionary<string, object?> { ["code"] = code } };
}

/// <summary>
/// Represents the resolved query root selection against the fixed Relay <c>Connection</c>/<c>Edge</c>/<c>PageInfo</c> shape.
/// </summary>
/// <param name="ConnectionTypeNameAlias">The requested <c>__typename</c> alias at the Connection level (or <see langword="null"/> where not selected).</param>
/// <param name="EdgesAlias">The requested alias for the <c>edges</c> field (or <see langword="null"/> where not selected).</param>
/// <param name="EdgeTypeNameAlias">The requested <c>__typename</c> alias at the Edge level (or <see langword="null"/> where not selected).</param>
/// <param name="NodeAlias">The requested alias for the <c>node</c> field (or <see langword="null"/> where not selected).</param>
/// <param name="NodeSelectionSet">The <c>node</c> field's own selection set, resolved separately against the item type via <see cref="GraphQLSelectionResolver"/>.</param>
/// <param name="CursorAlias">The requested alias for the <c>cursor</c> field (or <see langword="null"/> where not selected).</param>
/// <param name="PageInfoAlias">The requested alias for the <c>pageInfo</c> field (or <see langword="null"/> where not selected).</param>
/// <param name="PageInfoTypeNameAlias">The requested <c>__typename</c> alias at the PageInfo level (or <see langword="null"/> where not selected).</param>
/// <param name="PageInfoFieldAliases">The requested aliases for the selected PageInfo fields, keyed by real field name.</param>
/// <param name="TotalCountAlias">The requested alias for the <c>totalCount</c> field (or <see langword="null"/> where not selected).</param>
internal sealed record ConnectionSelection(string? ConnectionTypeNameAlias, string? EdgesAlias, string? EdgeTypeNameAlias, string? NodeAlias, GraphQLSelectionSet? NodeSelectionSet,
    string? CursorAlias, string? PageInfoAlias, string? PageInfoTypeNameAlias, IReadOnlyDictionary<string, string> PageInfoFieldAliases, string? TotalCountAlias);
