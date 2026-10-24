namespace CoreEx.Data.Querying;

/// <summary>
/// Represents a basic free-text query search (<c>$search</c>) parser with explicitly defined behavior.
/// </summary>
/// <remarks>The search text is <b>opaque</b>; i.e. no grammar (boolean operators, phrases, etc.) is defined or interpreted. The parser normalizes and validates the text (length, terms), and where the
/// <see cref="Mode"/> is <see cref="QuerySearchMode.Keyword"/> provides a provider-agnostic dynamic LINQ predicate across the configured <see cref="AddKeywordField(string, Action{QuerySearchFieldConfig}?)">keyword fields</see>.
/// <para>All other modes are <see cref="IsRanked">ranked</see> and are <b>not</b> executed by <i>CoreEx</i>; the repository is responsible for executing using the
/// <see cref="QuerySearchParserResult.Text"/> (e.g. generating an embedding and performing a vector similarity search), with results ordered by relevance.</para>
/// <para>This is <b>not</b> intended to be a replacement for a dedicated search engine; use <c>$filter</c> for precise, structured filtering.</para></remarks>
/// <param name="owner">The owning <see cref="QueryArgsConfig"/>.</param>
public sealed class QuerySearchParser(QueryArgsConfig owner)
{
    private readonly List<QuerySearchFieldConfig> _fields = [];
    private Action<QuerySearchParserResult>? _validator;
    private string? _helpText;

    /// <summary>
    /// Gets the default maximum search text length.
    /// </summary>
    public const int DefaultMaxLength = 200;

    /// <summary>
    /// Gets the default maximum number of <see cref="QuerySearchMode.Keyword"/> search terms.
    /// </summary>
    public const int DefaultMaxTerms = 8;

    /// <summary>
    /// Gets the owning <see cref="QueryArgsConfig"/>.
    /// </summary>
    public QueryArgsConfig Owner => owner.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="QuerySearchMode"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="QuerySearchMode.Keyword"/>.</remarks>
    public QuerySearchMode Mode { get; private set; } = QuerySearchMode.Keyword;

    /// <summary>
    /// Indicates whether the search is ranked (i.e. <see cref="Mode"/> is not <see cref="QuerySearchMode.Keyword"/>) and is to be executed by the repository with results ordered by relevance.
    /// </summary>
    public bool IsRanked => Mode != QuerySearchMode.Keyword;

    /// <summary>
    /// Gets the maximum (normalized) search text length.
    /// </summary>
    /// <remarks>Defaults to <see cref="DefaultMaxLength"/>.</remarks>
    public int MaxLength { get; private set; } = DefaultMaxLength;

    /// <summary>
    /// Gets the maximum number of distinct search terms; only enforced where the <see cref="Mode"/> is <see cref="QuerySearchMode.Keyword"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="DefaultMaxTerms"/>. Each term results in an additional predicate per keyword field, so this limits the resulting query complexity.</remarks>
    public int MaxTerms { get; private set; } = DefaultMaxTerms;

    /// <summary>
    /// Gets the default model prefix (if any).
    /// </summary>
    /// <remarks>This will be automatically applied to all subsequent field additions; for example <see cref="AddKeywordField(string, Action{QuerySearchFieldConfig}?)"/>.</remarks>
    public string? DefaultModelPrefix { get; private set; }

    /// <summary>
    /// Gets the configured <see cref="QuerySearchMode.Keyword"/> fields.
    /// </summary>
    public IReadOnlyList<QuerySearchFieldConfig> KeywordFields => _fields;

    /// <summary>
    /// Sets (overrides) the <see cref="Mode"/>.
    /// </summary>
    /// <param name="mode">The <see cref="QuerySearchMode"/>.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    public QuerySearchParser WithMode(QuerySearchMode mode)
    {
        Mode = mode;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="MaxLength"/>.
    /// </summary>
    /// <param name="maxLength">The maximum (normalized) search text length.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    public QuerySearchParser WithMaxLength(int maxLength)
    {
        MaxLength = maxLength.ThrowWhen(v => v <= 0, "Maximum length must be greater than zero.");
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="MaxTerms"/>.
    /// </summary>
    /// <param name="maxTerms">The maximum number of distinct search terms.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    public QuerySearchParser WithMaxTerms(int maxTerms)
    {
        MaxTerms = maxTerms.ThrowWhen(v => v <= 0, "Maximum terms must be greater than zero.");
        return this;
    }

    /// <summary>
    /// Sets (overrides) the <see cref="DefaultModelPrefix"/> (if any).
    /// </summary>
    /// <param name="modelPrefix">The model prefix.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    public QuerySearchParser WithDefaultModelPrefix(string? modelPrefix = null)
    {
        DefaultModelPrefix = modelPrefix;
        return this;
    }

    /// <summary>
    /// Adds a <see cref="QuerySearchMode.Keyword"/> <see cref="QuerySearchFieldConfig"/> for the specified <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The model name (must be of <see cref="Type"/> <see cref="string"/>) to be used for the dynamic LINQ expression.</param>
    /// <param name="configure">The optional action enabling further field configuration.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The model names are <b>not</b> exposed externally (e.g. via <see cref="ToJsonSchema"/>).</remarks>
    public QuerySearchParser AddKeywordField(string model, Action<QuerySearchFieldConfig>? configure = null)
    {
        var config = new QuerySearchFieldConfig(this, model);
        configure?.Invoke(config);
        config.FullyQualifiedModelName.ThrowWhen(name => _fields.Any(f => f.FullyQualifiedModelName == name), $"The search keyword field '{config.FullyQualifiedModelName}' has already been added and must be unique.");
        _fields.Add(config);
        return this;
    }

    /// <summary>
    /// Sets (override) the additional help text.
    /// </summary>
    /// <param name="text">The additional help text.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    public QuerySearchParser WithHelpText(string text)
    {
        _helpText = text;
        return this;
    }

    /// <summary>
    /// Sets (overrides) a <paramref name="validator"/> that can be used to further validate the parsed search.
    /// </summary>
    /// <param name="validator">The validator action.</param>
    /// <returns>The <see cref="QuerySearchParser"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Throw a <see cref="QuerySearchParserException"/> to have the validation message formatted correctly and consistently.</remarks>
    public QuerySearchParser WithValidator(Action<QuerySearchParserResult>? validator)
    {
        _validator = validator;
        return this;
    }

    /// <summary>
    /// Parses (normalizes and validates) the free-text <paramref name="search"/>.
    /// </summary>
    /// <param name="search">The free-text <c>$search</c> statement.</param>
    /// <returns>The <see cref="QuerySearchParserResult"/>.</returns>
    public QuerySearchParserResult Parse(string? search)
    {
        if (Mode == QuerySearchMode.Keyword && _fields.Count == 0)
            throw new InvalidOperationException($"A {nameof(QuerySearchMode.Keyword)} search requires at least one field to be configured; see {nameof(AddKeywordField)}.");

        var terms = string.IsNullOrWhiteSpace(search) ? [] : search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return new QuerySearchParserResult(this, null, []);

        var text = string.Join(' ', terms);

        try
        {
            if (text.Length > MaxLength)
                throw new QuerySearchParserException($"Statement must not exceed {MaxLength} characters in length.");

            var distinct = terms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (Mode == QuerySearchMode.Keyword && distinct.Length > MaxTerms)
                throw new QuerySearchParserException($"Statement must not contain more than {MaxTerms} distinct terms.");

            var result = new QuerySearchParserResult(this, text, distinct);
            _validator?.Invoke(result);
            return result;
        }
        catch (QuerySearchParserException qspex)
        {
            qspex.WithExtension("schema", owner.ToJsonSchema());
            return new QuerySearchParserResult(qspex);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Search is free-text (no operators or phrases); ");
        sb.Append(Mode switch
        {
            QuerySearchMode.FullText => "full-text search with results ordered by relevance (order-by is not supported).",
            QuerySearchMode.Semantic => "semantic (meaning-based) search with results ordered by relevance (order-by is not supported).",
            QuerySearchMode.Hybrid => "hybrid (full-text and semantic) search with results ordered by relevance (order-by is not supported).",
            _ => "keyword search where each term must match."
        });

        sb.AppendLine().Append("Maximum length: ").Append(MaxLength);
        if (Mode == QuerySearchMode.Keyword)
            sb.AppendLine().Append("Maximum terms: ").Append(MaxTerms);

        if (!string.IsNullOrEmpty(_helpText))
            sb.AppendLine().AppendLine("---").Append("Note: ").Append(_helpText);

        return sb.ToString();
    }

    /// <summary>
    /// Produces the JSON schema for the configuration.
    /// </summary>
    /// <returns>The <see cref="JsonElement"/>.</returns>
    public JsonElement ToJsonSchema()
    {
        var root = new Dictionary<string, object?>
        {
            { "mode", Mode.ToString().ToLowerInvariant() },
            { "ranked", IsRanked },
            { "maxLength", MaxLength }
        };

        if (Mode == QuerySearchMode.Keyword)
            root["maxTerms"] = MaxTerms;

        if (!string.IsNullOrEmpty(_helpText))
            root["description"] = _helpText;

        return JsonSerializer.SerializeToElement(root);
    }
}
