namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the concrete, transport-agnostic <see cref="IGraphQLEngine"/> implementation that bridges a GraphQL-lite document to the registered <see cref="GraphQLLiteOptions"/> query roots.
/// </summary>
/// <param name="options">The registered <see cref="GraphQLLiteOptions"/>.</param>
public sealed class GraphQLEngine(GraphQLLiteOptions options) : IGraphQLEngine
{
    private const string SchemaFieldName = "__schema";
    private const string TypeFieldName = "__type";
    private readonly GraphQLLiteOptions _options = options.ThrowIfNull();

    // Built once (not per-request) from the registered roots, which do not change for the lifetime of a registered GraphQLLiteOptions instance; see GraphQLIntrospectionSchemaBuilder.
    private readonly Lazy<GraphQLIntrospectionDocument> _introspection = new(() => GraphQLIntrospectionSchemaBuilder.Build(options, JsonDefaults.SerializerOptions));

    /// <inheritdoc/>
    public Task<JsonElement> GetSchemaAsync(CancellationToken cancellationToken = default)
        => GraphQLEngineInvoker.Default.InvokeAsync(this, (_, _) => Task.FromResult(_introspection.Value.Schema.Deserialize<JsonElement>()), cancellationToken);

    /// <inheritdoc/>
    public Task<GraphQLEngineResult> ExecuteAsync(string document, string? operationName = null, IReadOnlyDictionary<string, object?>? variables = null, CancellationToken cancellationToken = default)
        => GraphQLEngineInvoker.Default.InvokeAsync(this, (_, ct) => ExecuteAsyncInternalAsync(document, operationName, variables, ct), cancellationToken);

    /// <summary>
    /// Executes the specified GraphQL document, returning the resulting data and any errors.
    /// </summary>
    private async Task<GraphQLEngineResult> ExecuteAsyncInternalAsync(string document, string? operationName = null, IReadOnlyDictionary<string, object?>? variables = null, CancellationToken cancellationToken = default)
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

        if (_options.MaxRootFields is int maxRootFields && operation.SelectionSet.Selections.Count > maxRootFields)
            return GraphQLEngineResult.Failure(NewError($"The document selects {operation.SelectionSet.Selections.Count} root fields, exceeding the configured maximum of {maxRootFields}.",
                code: "TOO_MANY_ROOT_FIELDS"));

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
                dataObj[alias] = null;
                continue;
            }

            if (string.Equals(name, SchemaFieldName, StringComparison.Ordinal))
            {
                if (!_options.EnableIntrospection)
                {
                    errors.Add(NewError("Introspection is disabled; enable GraphQLLiteOptions.EnableIntrospection to query '__schema'.", [alias], "INTROSPECTION_DISABLED"));
                    dataObj[alias] = null;
                    continue;
                }

                // Meta-fields describing the schema itself: the full canonical __Schema/__Type shape is returned unconditionally (over-fetch), regardless of the client's nested
                // selection set - safe/expected for introspection, and avoids needing general fragment-spread support just for the standard client-tooling introspection query.
                try
                {
                    dataObj[alias] = _introspection.Value.Schema.DeepClone();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // _introspection is a Lazy<T> - it caches and rethrows any factory exception on every subsequent access, so this must be guarded the same way as every
                    // other root-field execution path rather than letting it escape ExecuteAsync as an unhandled exception.
                    errors.Add(MapException(ex, alias));
                    dataObj[alias] = null;
                }

                continue;
            }

            if (string.Equals(name, TypeFieldName, StringComparison.Ordinal))
            {
                if (!_options.EnableIntrospection)
                {
                    errors.Add(NewError("Introspection is disabled; enable GraphQLLiteOptions.EnableIntrospection to query '__type'.", [alias], "INTROSPECTION_DISABLED"));
                    dataObj[alias] = null;
                    continue;
                }

                try
                {
                    var typeName = args.GetString("name");
                    dataObj[alias] = typeName is not null && _introspection.Value.TypesByName.TryGetValue(typeName, out var typeNode) ? typeNode.DeepClone() : null;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors.Add(MapException(ex, alias));
                    dataObj[alias] = null;
                }

                continue;
            }

            if (_options.QueryRoots.TryGetValue(name, out var queryRoot))
                await ExecuteQueryRootAsync(queryRoot, field, alias, args, jsonOptions, dataObj, errors, cancellationToken).ConfigureAwait(false);
            else if (_options.ItemRoots.TryGetValue(name, out var itemRoot))
                await ExecuteItemRootAsync(itemRoot, field, alias, args, jsonOptions, dataObj, errors, cancellationToken).ConfigureAwait(false);
            else
                errors.Add(NewError($"Unknown root field '{name}'.", [alias], "UNKNOWN_ROOT"));

            if (!dataObj.ContainsKey(alias))
                dataObj[alias] = null;
        }

        var result = new GraphQLEngineResult();
        if (dataObj.Count > 0)
            result.Data = JsonSerializer.SerializeToElement(dataObj, jsonOptions);

        if (errors.Count > 0)
            result.Errors = errors;

        return result;
    }

    /// <summary>
    /// Executes a registered <see cref="GraphQLQueryRoot"/> (list query) as a Relay <see href="https://relay.dev/graphql/connections.htm">Cursor Connection</see> and adds the
    /// resulting <c>edges</c>/<c>pageInfo</c>/<c>totalCount</c> object to <paramref name="dataObj"/>.
    /// </summary>
    private Task ExecuteQueryRootAsync(GraphQLQueryRoot root, GraphQLField field, string alias, IReadOnlyDictionary<string, object?> args, JsonSerializerOptions jsonOptions, JsonObject dataObj, List<GraphQLEngineError> errors, CancellationToken cancellationToken)
        => GraphQLEngineInvoker.Default.InvokeAsync(this, async (tracer, ct) => 
        {
            tracer.Activity?.AddTag("graphql.operation.type", "query").AddTag("graphql.alias", alias);

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
                var queryArgs = GraphQLArgsMapper.BuildQueryArgs(args, root.QueryArgsConfig.HasFilterParser ? root.QueryArgsConfig.FilterParser : null);
                queryArgs.IncludeFields = paths.Count > 0 ? paths : null;
                var needsItems = connection.EdgesAlias is not null || connection.PageInfoAlias is not null;
                var (pagingArgs, first, requiresTotalCountForHasNextPage) = GraphQLArgsMapper.BuildConnectionPagingArgs(args, connection.TotalCountAlias is not null, needsItems);
                var skip = pagingArgs.Skip;

                var result = await root.InvokeAsync(queryArgs, pagingArgs, cancellationToken).ConfigureAwait(false);
                var allItems = result.Items?.Cast<object?>().ToList() ?? [];

                // Ordinarily hasNextPage is derived from the one-item over-fetch (Take = first + 1). Where PagingArgs.MaximumTake made that over-fetch impossible (see
                // GraphQLArgsMapper.BuildConnectionPagingArgs), fall back to comparing against the forced PagingResult.TotalCount instead.
                var hasNextPage = requiresTotalCountForHasNextPage && result.Paging?.TotalCount is long totalCount
                    ? skip + allItems.Count < totalCount
                    : allItems.Count > first;

                var pageItems = allItems.Count > first ? [.. allItems.Take(first)] : allItems;
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
        }, cancellationToken);

    /// <summary>
    /// Assembles the Relay Cursor Connection response object (<c>edges</c>/<c>pageInfo</c>/<c>totalCount</c>, plus any requested <c>__typename</c>s) per <paramref name="connection"/>.
    /// </summary>
    private static JsonObject BuildConnectionObject(ConnectionSelection connection, string itemTypeName, int itemCount, int skip, bool hasNextPage, bool hasPreviousPage, long? totalCount, JsonArray? shapedNodes)
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
                // fieldName retains the client's original casing (e.g. "HasNextPage") - compare case-insensitively, consistent with every other field name in the schema.
                pageInfoObj[fieldAlias] =
                    string.Equals(fieldName, GraphQLConnectionResolver.HasNextPageField, StringComparison.OrdinalIgnoreCase) ? hasNextPage
                    : string.Equals(fieldName, GraphQLConnectionResolver.HasPreviousPageField, StringComparison.OrdinalIgnoreCase) ? hasPreviousPage
                    : string.Equals(fieldName, GraphQLConnectionResolver.StartCursorField, StringComparison.OrdinalIgnoreCase) ? (itemCount > 0 ? GraphQLCursor.Encode(skip) : null)
                    : string.Equals(fieldName, GraphQLConnectionResolver.EndCursorField, StringComparison.OrdinalIgnoreCase) ? (itemCount > 0 ? GraphQLCursor.Encode(skip + itemCount - 1) : null)
                    : null;
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
    private Task ExecuteItemRootAsync(GraphQLItemRoot root, GraphQLField field, string alias, IReadOnlyDictionary<string, object?> args, JsonSerializerOptions jsonOptions, JsonObject dataObj, List<GraphQLEngineError> errors, CancellationToken cancellationToken)
        => GraphQLEngineInvoker.Default.InvokeAsync(this, async (tracer, ct) =>
        {
            tracer.Activity?.AddTag("graphql.operation.type", "query").AddTag("graphql.alias", alias);

            var (paths, selectionErrors) = GraphQLSelectionResolver.Resolve(field.SelectionSet, root.ItemType, jsonOptions, alias);
            if (selectionErrors.Count > 0)
            {
                errors.AddRange(selectionErrors);
                return;
            }

            try
            {
                GraphQLArgsMapper.ApplyItemRootFlags(args); // A single-item get does not support where/orderBy (rejected outright); includeText's ExecutionContext.IncludeRelatedText side-effect is still honored.

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
        }, cancellationToken);

    /// <summary>
    /// The configuration key controlling whether an unexpected (non-<see cref="IExtendedException"/>) exception's real message/detail is surfaced to the client, mirroring the identically-named
    /// REST convention (see <c>WebApiBase.IncludeExceptionInProblemDetailsName</c> in <c>CoreEx.AspNetCore</c>) so a single app setting governs both transports.
    /// </summary>
    private const string IncludeExceptionInProblemDetailsName = "CoreEx:IncludeExceptionInProblemDetails";

    /// <summary>
    /// Maps a thrown exception to a <see cref="GraphQLEngineError"/> with an appropriate error code.
    /// </summary>
    /// <remarks><see cref="ArgumentException"/> (and <see cref="ArgumentNullException"/>/<see cref="KeyNotFoundException"/>) are included alongside <see cref="GraphQLArgumentTranslationException"/>
    /// as <c>ARGUMENT_ERROR</c> since a registered resolver delegate (e.g. an <c>AddGet</c> item root) commonly throws one of these standard .NET exception types itself to reject a
    /// missing/invalid argument - without this, such resolver-thrown argument problems would be indistinguishable from a genuine server-side execution fault. These, and the parser exceptions,
    /// are considered "expected" client-argument problems and are never logged. <see cref="IExtendedException"/>-derived exceptions (known business exceptions) surface their own safe
    /// <see cref="Exception.Message"/> and are logged only where <see cref="IExtendedException.ShouldBeLogged"/> (config-gated, default <see langword="false"/>) - mirroring the REST
    /// <c>WebApi</c> convention. Any other (genuinely unexpected) exception is always logged, and its real message is only surfaced to the client where
    /// <c>CoreEx:IncludeExceptionInProblemDetails</c> is explicitly enabled; otherwise a generic <see cref="UnexpectedInternalException"/> message is returned - again mirroring <c>WebApi</c>.</remarks>
    private static GraphQLEngineError MapException(Exception ex, string alias) => ex switch
    {
        GraphQLArgumentTranslationException => NewError(ex.Message, [alias], "ARGUMENT_ERROR"),
        ArgumentException => NewError(ex.Message, [alias], "ARGUMENT_ERROR"),
        KeyNotFoundException => NewError(ex.Message, [alias], "ARGUMENT_ERROR"),
        QueryFilterParserException => NewError(ex.Message, [alias], "FILTER_PARSE_ERROR"),
        QueryOrderByParserException => NewError(ex.Message, [alias], "ORDERBY_PARSE_ERROR"),
        ValidationException vex => MapValidationException(vex, alias),
        NotFoundException => MapKnownExtendedException(ex, alias, "NOT_FOUND"),
        ConflictException => MapKnownExtendedException(ex, alias, "CONFLICT_ERROR"),
        DuplicateException => MapKnownExtendedException(ex, alias, "DUPLICATE_ERROR"),
        ConcurrencyException => MapKnownExtendedException(ex, alias, "CONCURRENCY_ERROR"),
        AuthenticationException => MapKnownExtendedException(ex, alias, "AUTHENTICATION_ERROR"),
        AuthorizationException => MapKnownExtendedException(ex, alias, "AUTHORIZATION_ERROR"),
        BusinessException => MapKnownExtendedException(ex, alias, "BUSINESS_ERROR"),
        IExtendedException => MapKnownExtendedException(ex, alias, "EXECUTION_ERROR"),
        _ => MapUnexpectedException(ex, alias)
    };

    /// <summary>
    /// Maps a known/expected <see cref="IExtendedException"/> (e.g. <see cref="NotFoundException"/>, <see cref="ConflictException"/>) to a <see cref="GraphQLEngineError"/>, logging it only
    /// where <see cref="IExtendedException.ShouldBeLogged"/> - these are "expected" business-flow exceptions, quiet by default (matching the REST <c>WebApi</c> convention).
    /// </summary>
    private static GraphQLEngineError MapKnownExtendedException(Exception ex, string alias, string code)
    {
        if (ex is IExtendedException eex && eex.ShouldBeLogged)
            LogException(ex);

        return NewError(ex.Message, [alias], code);
    }

    /// <summary>
    /// Maps a <see cref="ValidationException"/> to a <see cref="GraphQLEngineError"/>, including a per-property <c>messages</c> extension (mirroring the REST <c>ValidationProblem</c> shape)
    /// where <see cref="ValidationException.Messages"/> is populated - so structured per-property detail is not lost behind the single top-level <see cref="Exception.Message"/>.
    /// </summary>
    private static GraphQLEngineError MapValidationException(ValidationException vex, string alias)
    {
        if (vex.ShouldBeLogged)
            LogException(vex);

        if (vex.Messages is not { Count: > 0 })
            return NewError(vex.Message, [alias], "VALIDATION_ERROR");

        var messages = new Dictionary<string, string[]>();
        foreach (var group in from m in vex.Messages.GetMessagesForType(MessageType.Error).Where(x => x.Property is not null && x.Text is not null)
                               group m by m.Property into g
                               select new { Property = g.Key, Messages = g })
        {
            messages.Add(group.Property!, [.. group.Messages.Select(m => m.Text!.ToString()!)]);
        }

        return NewError(vex.Message, [alias], "VALIDATION_ERROR", new Dictionary<string, object?> { ["messages"] = messages });
    }

    /// <summary>
    /// Maps a genuinely unexpected (non-<see cref="IExtendedException"/>) exception to a generic <c>EXECUTION_ERROR</c>, always logging it (unlike known/expected exceptions) and only
    /// surfacing its real message/detail where <c>CoreEx:IncludeExceptionInProblemDetails</c> is explicitly enabled - otherwise a generic <see cref="UnexpectedInternalException"/> message is
    /// returned, matching the REST <c>WebApi</c> convention so an unhandled fault (e.g. a <see cref="NullReferenceException"/> or database error) never leaks server internals by default.
    /// </summary>
    private static GraphQLEngineError MapUnexpectedException(Exception ex, string alias)
    {
        LogException(ex);

        var includeDetail = CoreEx.Abstractions.Internal.GetConfigurationValue(IncludeExceptionInProblemDetailsName, false);
        var message = includeDetail ? ex.Message : new UnexpectedInternalException().Message;
        return NewError(message, [alias], "EXECUTION_ERROR");
    }

    /// <summary>
    /// Logs the specified <paramref name="ex"/> as an error via the ambient <see cref="ILogger{GraphQLEngine}"/> (where available), using the same <see cref="CoreEx.ExecutionContext.HasCurrent"/>/
    /// <see cref="CoreEx.ExecutionContext.GetService{T}"/> pattern established in <see cref="GraphQLQueryRoot"/>.
    /// </summary>
    private static void LogException(Exception ex)
    {
        if (!ExecutionContext.HasCurrent)
            return;

        var logger = ExecutionContext.GetService<ILogger<GraphQLEngine>>();
        if (logger is not null && logger.IsEnabled(LogLevel.Error))
            logger.LogError(ex, "{Error}", ex.Message);
    }

    /// <summary>
    /// Creates a new <see cref="GraphQLEngineError"/> with the specified message, path, error code and any additional <paramref name="extraExtensions"/>.
    /// </summary>
    private static GraphQLEngineError NewError(string message, IReadOnlyList<string>? path = null, string? code = null, IReadOnlyDictionary<string, object?>? extraExtensions = null)
    {
        Dictionary<string, object?>? extensions = null;
        if (code is not null || extraExtensions is not null)
        {
            extensions = code is null ? [] : new Dictionary<string, object?> { ["code"] = code };
            if (extraExtensions is not null)
            {
                foreach (var kvp in extraExtensions)
                    extensions[kvp.Key] = kvp.Value;
            }
        }

        return new(message) { Path = path, Extensions = extensions };
    }
}
