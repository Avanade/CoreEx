namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the concrete, transport-agnostic <see cref="IGraphQLEngine"/> implementation that bridges a GraphQL-lite document to the registered <see cref="GraphQLLiteOptions"/> query roots.
/// </summary>
/// <param name="options">The registered <see cref="GraphQLLiteOptions"/>.</param>
public sealed class GraphQLEngine(GraphQLLiteOptions options) : IGraphQLEngine
{
    private const string SchemaFieldName = "__schema";
    private readonly GraphQLLiteOptions _options = options.ThrowIfNull();

    /// <inheritdoc/>
    public Task<JsonElement> GetSchemaAsync(CancellationToken cancellationToken = default) => Task.FromResult(GraphQLSchemaBuilder.Build(_options, JsonDefaults.SerializerOptions));

    /// <inheritdoc/>
    public async Task<GraphQLEngineResult> ExecuteAsync(string document, string? operationName = null, IReadOnlyDictionary<string, object?>? variables = null, CancellationToken cancellationToken = default)
    {
        document.ThrowIfNull();

        GraphQLDocument parsed;
        try
        {
            parsed = Parser.Parse(document);
        }
        catch (GraphQLSyntaxErrorException ex)
        {
            return GraphQLEngineResult.Failure(NewError(ex.Message, code: "SYNTAX_ERROR"));
        }

        var operations = parsed.Definitions.OfType<GraphQLOperationDefinition>().ToList();
        if (operations.Count == 0)
            return GraphQLEngineResult.Failure(NewError("The GraphQL document does not contain an operation.", code: "SYNTAX_ERROR"));

        GraphQLOperationDefinition operation;
        if (!string.IsNullOrEmpty(operationName))
        {
            var match = operations.FirstOrDefault(o => string.Equals(o.Name?.StringValue, operationName, StringComparison.Ordinal));
            if (match is null)
                return GraphQLEngineResult.Failure(NewError($"No operation named '{operationName}' was found in the document.", code: "OPERATION_NOT_FOUND"));

            operation = match;
        }
        else if (operations.Count == 1)
            operation = operations[0];
        else
            return GraphQLEngineResult.Failure(NewError("An operation name is required where the document contains multiple operations.", code: "OPERATION_NAME_REQUIRED"));

        if (operation.Operation != GraphQLParser.AST.OperationType.Query)
            return GraphQLEngineResult.Failure(NewError("Only query operations are supported; mutations and subscriptions are not supported.", code: "OPERATION_NOT_SUPPORTED"));

        var jsonOptions = JsonDefaults.SerializerOptions;
        var dataObj = new JsonObject();
        var errors = new List<GraphQLEngineError>();
        var seenAliases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var selection in operation.SelectionSet.Selections)
        {
            if (selection is not GraphQLField field)
            {
                errors.Add(NewError("Fragment spreads and inline fragments are not supported.", code: "FRAGMENTS_NOT_SUPPORTED"));
                continue;
            }

            var name = field.Name.StringValue;
            var alias = field.Alias?.Name.StringValue ?? name;

            if (!seenAliases.Add(alias))
            {
                // GraphQL-lite does not implement full spec field-merging for repeated response keys; reject rather than silently letting the last selection win.
                errors.Add(NewError($"Response key '{alias}' is selected more than once at the root; use a distinct alias for each occurrence.", [alias], "DUPLICATE_FIELD"));
                continue;
            }

            if (name == GraphQLSelectionResolver.TypeNameField)
            {
                dataObj[alias] = "Query";
                continue;
            }

            if (string.Equals(name, SchemaFieldName, StringComparison.Ordinal))
            {
                dataObj[alias] = JsonNode.Parse((await GetSchemaAsync(cancellationToken).ConfigureAwait(false)).GetRawText());
                continue;
            }

            IReadOnlyDictionary<string, object?> args;
            try
            {
                args = GraphQLValueConverter.ConvertArguments(field.Arguments, variables);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // An undefined variable reference (or other argument-shape error) is raised while converting arguments, before a root is even resolved; map it the same way as
                // an error thrown during root invocation rather than letting it escape ExecuteAsync unhandled.
                errors.Add(MapException(ex, alias));
                continue;
            }

            if (_options.QueryRoots.TryGetValue(name, out var queryRoot))
                await ExecuteQueryRootAsync(queryRoot, field, alias, args, jsonOptions, dataObj, errors, cancellationToken).ConfigureAwait(false);
            else if (_options.ItemRoots.TryGetValue(name, out var itemRoot))
                await ExecuteItemRootAsync(itemRoot, field, alias, args, jsonOptions, dataObj, errors, cancellationToken).ConfigureAwait(false);
            else
                errors.Add(NewError($"Unknown root field '{name}'.", [alias], "UNKNOWN_ROOT"));
        }

        var result = new GraphQLEngineResult();
        if (dataObj.Count > 0)
            result.Data = JsonSerializer.SerializeToElement(dataObj);

        if (errors.Count > 0)
            result.Errors = errors;

        return result;
    }

    /// <summary>
    /// Executes a registered <see cref="GraphQLQueryRoot"/> (list query) as a Relay <see href="https://relay.dev/graphql/connections.htm">Cursor Connection</see> and adds the
    /// resulting <c>edges</c>/<c>pageInfo</c>/<c>totalCount</c> object to <paramref name="dataObj"/>.
    /// </summary>
    private static async Task ExecuteQueryRootAsync(GraphQLQueryRoot root, GraphQLField field, string alias, IReadOnlyDictionary<string, object?> args, JsonSerializerOptions jsonOptions,
        JsonObject dataObj, List<GraphQLEngineError> errors, CancellationToken cancellationToken)
    {
        var (resolvedConnection, connectionErrors) = GraphQLConnectionResolver.Resolve(field.SelectionSet, [alias]);
        if (connectionErrors.Count > 0 || resolvedConnection is null)
        {
            errors.AddRange(connectionErrors);
            return;
        }

        var connection = resolvedConnection;
        var itemFieldMap = GraphQLTypeShape.GetFieldMap(root.ItemType, jsonOptions);
        var paths = new List<string>();
        if (connection.NodeAlias is not null)
        {
            // Use the client's requested aliases (not the fixed 'edges'/'node' field names) so a nested selection error's Path matches the actual response JSON.
            var (nodePaths, nodeErrors) = GraphQLSelectionResolver.Resolve(connection.NodeSelectionSet, root.ItemType, jsonOptions, GraphQLConnectionResolver.NodeField,
                [alias, connection.EdgesAlias ?? GraphQLConnectionResolver.EdgesField, connection.NodeAlias]);
            if (nodeErrors.Count > 0)
            {
                errors.AddRange(nodeErrors);
                return;
            }

            paths.AddRange(nodePaths);
        }

        try
        {
            var queryArgs = GraphQLArgsMapper.BuildQueryArgs(args);
            var needsItems = connection.EdgesAlias is not null || connection.PageInfoAlias is not null;
            var (pagingArgs, first) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, connection.TotalCountAlias is not null, needsItems);
            var skip = pagingArgs.Skip;

            var result = await root.InvokeAsync(queryArgs, pagingArgs, cancellationToken).ConfigureAwait(false);
            var allItems = result.Items?.Cast<object?>().ToList() ?? [];
            var hasNextPage = allItems.Count > first;
            var pageItems = hasNextPage ? allItems.Take(first).ToList() : allItems;
            var hasPreviousPage = skip > 0;

            JsonArray? shapedNodes = null;
            if (connection.NodeAlias is not null)
            {
                var pageItemsJson = JsonSerializer.Serialize(pageItems, jsonOptions);
                JsonFilter.TryJsonFilter(pageItemsJson, paths, out var filteredJson, JsonFilterOption.Include, jsonOptions);
                shapedNodes = GraphQLResponseShaper.Shape(JsonNode.Parse(filteredJson), connection.NodeSelectionSet, itemFieldMap, root.ItemType.Name) as JsonArray;
            }

            dataObj[alias] = BuildConnectionObject(connection, root.ItemType.Name, pageItems.Count, skip, hasNextPage, hasPreviousPage, result.Paging?.TotalCount, shapedNodes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add(MapException(ex, alias));
        }
    }

    /// <summary>
    /// Assembles the Relay Cursor Connection response object (<c>edges</c>/<c>pageInfo</c>/<c>totalCount</c>, plus any requested <c>__typename</c>s) per <paramref name="connection"/>.
    /// </summary>
    private static JsonObject BuildConnectionObject(ConnectionSelection connection, string itemTypeName, int itemCount, int skip, bool hasNextPage, bool hasPreviousPage,
        long? totalCount, JsonArray? shapedNodes)
    {
        var connectionObj = new JsonObject();

        if (connection.ConnectionTypeNameAlias is not null)
            connectionObj[connection.ConnectionTypeNameAlias] = $"{itemTypeName}Connection";

        if (connection.EdgesAlias is not null)
        {
            var edgesArray = new JsonArray();
            for (var i = 0; i < itemCount; i++)
            {
                var edgeObj = new JsonObject();

                if (connection.EdgeTypeNameAlias is not null)
                    edgeObj[connection.EdgeTypeNameAlias] = $"{itemTypeName}Edge";

                if (connection.NodeAlias is not null)
                    edgeObj[connection.NodeAlias] = shapedNodes?[i]?.DeepClone();

                if (connection.CursorAlias is not null)
                    edgeObj[connection.CursorAlias] = GraphQLCursor.Encode(skip + i);

                edgesArray.Add(edgeObj);
            }

            connectionObj[connection.EdgesAlias] = edgesArray;
        }

        if (connection.PageInfoAlias is not null)
        {
            var pageInfoObj = new JsonObject();

            if (connection.PageInfoTypeNameAlias is not null)
                pageInfoObj[connection.PageInfoTypeNameAlias] = "PageInfo";

            foreach (var (fieldName, fieldAlias) in connection.PageInfoFieldAliases)
            {
                pageInfoObj[fieldAlias] = fieldName switch
                {
                    GraphQLConnectionResolver.HasNextPageField => hasNextPage,
                    GraphQLConnectionResolver.HasPreviousPageField => hasPreviousPage,
                    GraphQLConnectionResolver.StartCursorField => itemCount > 0 ? GraphQLCursor.Encode(skip) : null,
                    GraphQLConnectionResolver.EndCursorField => itemCount > 0 ? GraphQLCursor.Encode(skip + itemCount - 1) : null,
                    _ => null
                };
            }

            connectionObj[connection.PageInfoAlias] = pageInfoObj;
        }

        if (connection.TotalCountAlias is not null)
            connectionObj[connection.TotalCountAlias] = totalCount;

        return connectionObj;
    }

    /// <summary>
    /// Executes a registered <see cref="GraphQLItemRoot"/> (single-item get) and adds the resulting filtered JSON to <paramref name="dataObj"/>.
    /// </summary>
    private static async Task ExecuteItemRootAsync(GraphQLItemRoot root, GraphQLField field, string alias, IReadOnlyDictionary<string, object?> args, JsonSerializerOptions jsonOptions,
        JsonObject dataObj, List<GraphQLEngineError> errors, CancellationToken cancellationToken)
    {
        var (paths, selectionErrors) = GraphQLSelectionResolver.Resolve(field.SelectionSet, root.ItemType, jsonOptions, alias);
        if (selectionErrors.Count > 0)
        {
            errors.AddRange(selectionErrors);
            return;
        }

        try
        {
            var item = await root.InvokeAsync(args, cancellationToken).ConfigureAwait(false);
            if (item is null)
            {
                // Mirror CoreEx's WebApi REST convention where a null result is treated as not-found (404), rather than surfacing a bare GraphQL null.
                errors.Add(NewError($"'{alias}' was not found.", [alias], "NOT_FOUND"));
                return;
            }

            var itemJson = JsonSerializer.Serialize(item, jsonOptions);
            JsonFilter.TryJsonFilter(itemJson, paths, out var filteredJson, JsonFilterOption.Include, jsonOptions);
            dataObj[alias] = GraphQLResponseShaper.Shape(JsonNode.Parse(filteredJson), field.SelectionSet, GraphQLTypeShape.GetFieldMap(root.ItemType, jsonOptions), root.ItemType.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add(MapException(ex, alias));
        }
    }

    /// <summary>
    /// Maps a thrown exception to a <see cref="GraphQLEngineError"/> with an appropriate error code.
    /// </summary>
    private static GraphQLEngineError MapException(Exception ex, string alias) => ex switch
    {
        GraphQLArgumentTranslationException => NewError(ex.Message, [alias], "ARGUMENT_ERROR"),
        QueryFilterParserException => NewError(ex.Message, [alias], "FILTER_PARSE_ERROR"),
        QueryOrderByParserException => NewError(ex.Message, [alias], "ORDERBY_PARSE_ERROR"),
        NotFoundException => NewError(ex.Message, [alias], "NOT_FOUND"),
        ValidationException => NewError(ex.Message, [alias], "VALIDATION_ERROR"),
        _ => NewError(ex.Message, [alias], "EXECUTION_ERROR")
    };

    /// <summary>
    /// Creates a new <see cref="GraphQLEngineError"/> with the specified message, path and error code.
    /// </summary>
    private static GraphQLEngineError NewError(string message, IReadOnlyList<string>? path = null, string? code = null) =>
        new(message) { Path = path, Extensions = code is null ? null : new Dictionary<string, object?> { ["code"] = code } };
}
