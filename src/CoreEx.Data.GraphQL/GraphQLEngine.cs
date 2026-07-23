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

        foreach (var selection in operation.SelectionSet.Selections)
        {
            if (selection is not GraphQLField field)
                continue; // fragments/inline-fragments not supported in v1 (non-goal) - silently skipped.

            var name = field.Name.StringValue;
            var alias = field.Alias?.Name.StringValue ?? name;

            if (string.Equals(name, SchemaFieldName, StringComparison.Ordinal))
            {
                dataObj[alias] = JsonNode.Parse((await GetSchemaAsync(cancellationToken).ConfigureAwait(false)).GetRawText());
                continue;
            }

            var args = GraphQLValueConverter.ConvertArguments(field.Arguments, variables);

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
    /// Executes a registered <see cref="GraphQLQueryRoot"/> (list query) and adds the resulting filtered JSON (and any paging metadata) to <paramref name="dataObj"/>.
    /// </summary>
    private static async Task ExecuteQueryRootAsync(GraphQLQueryRoot root, GraphQLField field, string alias, IReadOnlyDictionary<string, object?> args, JsonSerializerOptions jsonOptions,
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
            var queryArgs = GraphQLArgsMapper.BuildQueryArgs(args);
            var pagingArgs = GraphQLArgsMapper.BuildPagingArgs(args);
            var result = await root.InvokeAsync(queryArgs, pagingArgs, cancellationToken).ConfigureAwait(false);

            var itemsJson = JsonSerializer.Serialize(result.Items, jsonOptions);
            JsonFilter.TryJsonFilter(itemsJson, paths, out var filteredJson, JsonFilterOption.Include, jsonOptions);
            dataObj[alias] = JsonNode.Parse(filteredJson);

            if (result.Paging is not null)
                dataObj[$"{alias}_paging"] = new JsonObject { ["skip"] = result.Paging.Skip, ["take"] = result.Paging.Take, ["totalCount"] = result.Paging.TotalCount };
        }
        catch (Exception ex)
        {
            errors.Add(MapException(ex, alias));
        }
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
            dataObj[alias] = JsonNode.Parse(filteredJson);
        }
        catch (Exception ex)
        {
            errors.Add(MapException(ex, alias));
        }
    }

    /// <summary>
    /// Maps a thrown exception to a <see cref="GraphQLEngineError"/> with an appropriate error code.
    /// </summary>
    private static GraphQLEngineError MapException(Exception ex, string alias) => ex switch
    {
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
