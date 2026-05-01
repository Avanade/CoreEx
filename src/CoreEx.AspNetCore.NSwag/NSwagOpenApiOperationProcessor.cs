namespace CoreEx.AspNetCore.NSwag;

/// <summary>
/// Provides the <i>NSwag</i> <see cref="IOperationProcessor"/> implementation that leverages the <see cref="OpenApiOptions"/> and applies the relevant attributes to the generated output.
/// </summary>
/// <param name="options">The <see cref="OpenApiOptions"/>.</param>
internal sealed class NSwagOpenApiOperationProcessor(OpenApiOptions options) : IOperationProcessor
{
    /// <summary>
    /// Gets the <see cref="OpenApiOptions"/>.
    /// </summary>
    public OpenApiOptions Options { get; } = options.ThrowIfNull();

    /// <inheritdoc/>
    public bool Process(OperationProcessorContext context)
    {
        if (context is AspNetCoreOperationProcessorContext ctx)
        {
            HandleAcceptsAttribute(ctx);
            HandleQueryAttribute(ctx);
            HandlePagingAttribute(ctx);
            HandleIdempotencyKeyAttribute(ctx);
            HandleProducesNotFoundProblemAttribute(ctx);
            HandleResponseProblemDetails(ctx);

            if (Options.IncludeFieldsRequestHeaders)
                HandleRequestFieldQueryString(ctx);

            if (Options.IncludeMessagesResponseHeaders) 
                HandleMessagesResponseHeaders(ctx);
        }

        return true;
    }

    /// <summary>
    /// Handles the <see cref="AcceptsAttribute"/>.
    /// </summary>
    private static void HandleAcceptsAttribute(AspNetCoreOperationProcessorContext context)
    {
        var accepts = (AcceptsAttribute?)context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is AcceptsAttribute);
        if (accepts is null)
            return;

        var body = new OpenApiRequestBody();
        context.OperationDescription.Operation.RequestBody = body;

        var schema = GetOrGenerateSchemaForType(context, accepts.BodyType);
        foreach (var contentType in new[] { accepts.ContentType }.Concat(accepts.AdditionalContentTypes ?? []))
        {
            body.Content.Add(contentType, new OpenApiMediaType { Schema = schema });
        }
    }

    /// <summary>
    /// Handles the <see cref="QueryAttribute"/>.
    /// </summary>
    private void HandleQueryAttribute(AspNetCoreOperationProcessorContext context)
    {
        var query = (QueryAttribute?)context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is QueryAttribute);
        if (query is null)
            return;

        if (query.SupportsFilter)
            context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.QueryFilterQueryStringName, Options.QueryFilterText, JsonObjectType.String, $"{nameof(QueryArgs)}{nameof(QueryArgs.Filter)}"));

        if (query.SupportsOrderBy)
            context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.QueryOrderByQueryStringName, Options.QueryOrderByText, JsonObjectType.String, $"{nameof(QueryArgs)}{nameof(QueryArgs.OrderBy)}"));
    }

    /// <summary>
    /// Handles the <see cref="PagingAttribute"/>.
    /// </summary>
    private void HandlePagingAttribute(AspNetCoreOperationProcessorContext context)
    {
        var paging = (PagingAttribute?)context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is PagingAttribute);
        if (paging is null)
            return;

        context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.PagingSkipQueryStringName, Options.PagingSkipText, JsonObjectType.Integer, $"{nameof(PagingArgs)}{nameof(PagingArgs.Skip)}"));
        context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.PagingTakeQueryStringName, Options.PagingTakeText, JsonObjectType.Integer, $"{nameof(PagingArgs)}{nameof(PagingArgs.Take)}"));
        if (paging.SupportsCount)
            context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.PagingCountQueryStringName, Options.PagingCountText, JsonObjectType.Boolean, $"{nameof(PagingArgs)}{nameof(ITotalCount.IsCountRequested)}"));

        if (Options.IncludePagingResponseHeaders)
        {
            foreach (var r in context.OperationDescription.Operation.Responses.Where(r => int.TryParse(r.Key, out var code) && code >= 200 && code < 300))
            {
                r.Value.Headers.Add(HttpNames.PagingSkipHeaderName, new OpenApiHeader { Schema = new JsonSchema { Type = JsonObjectType.Integer }, OriginalName = $"{nameof(PagingResult)}{nameof(PagingResult.Skip)}", Description = Options.PagingSkipText });
                r.Value.Headers.Add(HttpNames.PagingTakeHeaderName, new OpenApiHeader { Schema = new JsonSchema { Type = JsonObjectType.Integer }, OriginalName = $"{nameof(PagingResult)}{nameof(PagingResult.Take)}", Description = Options.PagingTakeText });
                if (paging.SupportsCount)
                    r.Value.Headers.Add(HttpNames.PagingTotalCountHeaderName, new OpenApiHeader { Schema = new JsonSchema { Type = JsonObjectType.Integer, IsNullableRaw = true }, OriginalName = $"{nameof(PagingResult)}{nameof(PagingResult.TotalCount)}", Description = Options.PagingTotalCountText });
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="IdempotencyKeyAttribute"/>.
    /// </summary>
    /// <param name="context"></param>
    private void HandleIdempotencyKeyAttribute(AspNetCoreOperationProcessorContext context)
    {
        var idempotencyKey = (IdempotencyKeyAttribute?)context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is IdempotencyKeyAttribute);
        if (idempotencyKey is null)
            return;

        context.OperationDescription.Operation.Parameters.Add(new OpenApiParameter
        {
            Name = HttpNames.IdempotencyKeyHeaderName,
            OriginalName = nameof(IdempotencyKey),
            Description = Options.IdempotencyKeyText,
            Kind = OpenApiParameterKind.Header,
            Schema = new JsonSchema { Type = JsonObjectType.String, MinLength = 8, MaxLength = 128, IsNullableRaw = !idempotencyKey.IsRequired }
        });
    }

    /// <summary>
    /// Handles the <see cref="ProducesNotFoundProblemAttribute"/>.
    /// </summary>
    private static void HandleProducesNotFoundProblemAttribute(AspNetCoreOperationProcessorContext context)
    {
        var notFoundProblem = (ProducesNotFoundProblemAttribute?)context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x => x is ProducesNotFoundProblemAttribute);
        if (notFoundProblem is null)
            return;

        // Add the NotFound ProblemDetails response.
        var schema = GetOrGenerateSchemaForType(context, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails));
        var key = ((int)HttpStatusCode.NotFound).ToString();
        if (!context.OperationDescription.Operation.Responses.ContainsKey(key))
            context.OperationDescription.Operation.Responses[key] = new OpenApiResponse { Content = { [MediaTypeNames.Application.ProblemJson] = new OpenApiMediaType { Schema = schema } } };
    }

    /// <summary>
    /// Handles the response problems.
    /// </summary>
    private void HandleResponseProblemDetails(AspNetCoreOperationProcessorContext context)
    {
        if (options.IncludeValidationProblemDetailsHttpStatusCodes)
        {
            var schema = GetOrGenerateSchemaForType(context, typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails));

            foreach (var sc in options.ValidationProblemDetailsHttpStatusCodes)
            {
                var key = ((int)sc).ToString();
                if (!context.OperationDescription.Operation.Responses.ContainsKey(key))
                    context.OperationDescription.Operation.Responses[key] = new OpenApiResponse { Content = { [MediaTypeNames.Application.ProblemJson] = new OpenApiMediaType { Schema = schema } } };
            }
        }

        if (options.IncludeProblemDetailsHttpStatusCodes)
        {
            var schema = GetOrGenerateSchemaForType(context, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails));

            foreach (var sc in options.ProblemDetailsHttpStatusCodes)
            {
                var key = ((int)sc).ToString();
                if (!context.OperationDescription.Operation.Responses.ContainsKey(key))
                    context.OperationDescription.Operation.Responses[key] = new OpenApiResponse { Content = { [MediaTypeNames.Application.ProblemJson] = new OpenApiMediaType { Schema = schema } } };
            }
        }
    }

    /// <summary>
    /// Gets or generates the schema for the specified <paramref name="type"/>.
    /// </summary>
    private static JsonSchema GetOrGenerateSchemaForType(AspNetCoreOperationProcessorContext context, Type type)
    {
        var schema = context.SchemaGenerator.Generate(type, context.SchemaResolver);

        if (schema.Reference is not null)
            return new JsonSchema { Reference = schema.Reference };

        var name = schema.Title ?? type.Name;
        context.Document.Components.Schemas[name] = schema.ActualSchema;
        return new JsonSchema { Reference = schema.ActualSchema };
    }

    /// <summary>
    /// Handles the <see cref="HttpNames.WarningMessagesHeaderName"/> and <see cref="HttpNames.InfoMessagesHeaderName"/> response headers.
    /// </summary>
    private void HandleMessagesResponseHeaders(AspNetCoreOperationProcessorContext context)
    {
        foreach (var r in context.OperationDescription.Operation.Responses.Where(x => int.TryParse(x.Key, out var sc) && sc < 400))
        {
            r.Value.Headers.Add(HttpNames.WarningMessagesHeaderName, new OpenApiHeader { Schema = new JsonSchema { Type = JsonObjectType.Array, Item = new JsonSchema { Type = JsonObjectType.String } }, OriginalName = "WarningMessages", Description = Options.WarningMessagesText });
            r.Value.Headers.Add(HttpNames.InfoMessagesHeaderName, new OpenApiHeader { Schema = new JsonSchema { Type = JsonObjectType.Array, Item = new JsonSchema { Type = JsonObjectType.String } }, OriginalName = "InfoMessages", Description = Options.InfoMessagesText });
        }
    }

    /// <summary>
    /// Handles the <see cref="HttpNames.IncludeFieldsQueryStringName"/> and <see cref="HttpNames.ExcludeFieldsQueryStringName"/>.
    /// </summary>
    private void HandleRequestFieldQueryString(AspNetCoreOperationProcessorContext context)
    {
        if (context.OperationDescription.Method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase) && context.OperationDescription.Operation.Responses.Any(x => int.TryParse(x.Key, out var sc) && sc < 400 && x.Value.Content.ContainsKey(MediaTypeNames.Application.Json)))
        {
            context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.IncludeFieldsQueryStringName, Options.IncludeFieldsText, JsonObjectType.String, $"{nameof(PagingArgs)}{nameof(QueryArgs.IncludeFields)}"));
            context.OperationDescription.Operation.Parameters.Add(CreateParameter(HttpNames.ExcludeFieldsQueryStringName, Options.ExcludeFieldsText, JsonObjectType.String, $"{nameof(PagingArgs)}{nameof(QueryArgs.ExcludeFields)}"));
        }
    }

    /// <summary>
    /// Create the parameter definition.
    /// </summary>
    private static OpenApiParameter CreateParameter(string name, string description, JsonObjectType type, string? original = null) => new()
    {
        Name = name,
        OriginalName = original ?? name,
        Description = description,
        Kind = OpenApiParameterKind.Query,
        Schema = new JsonSchema { Type = type }
    };
}