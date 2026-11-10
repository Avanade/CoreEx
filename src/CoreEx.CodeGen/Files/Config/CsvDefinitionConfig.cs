namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines CSV syntax, routing, and hierarchy settings.</summary>
[CodeGenClass("Csv", Title = "CSV parser and assembly settings.")]
[CodeGenCategory("Dialect", Title = "CSV dialect and explicit encoding.")]
[CodeGenCategory("Routing", Title = "CSV routing and hierarchy.")]
public class CsvDefinitionConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the delimiter.</summary>
    [JsonPropertyName("delimiter")]
    [CodeGenProperty("Dialect", Title = "Delimiter.", Description = "One character; defaults to comma.")]
    public string? Delimiter { get; set; }

    /// <summary>Gets or sets the quote.</summary>
    [JsonPropertyName("quote")]
    [CodeGenProperty("Dialect", Title = "Quote.", Description = "One character; defaults to double quote.")]
    public string? Quote { get; set; }

    /// <summary>Gets or sets the optional comment marker.</summary>
    [JsonPropertyName("comment")]
    [CodeGenProperty("Dialect", Title = "Comment marker.", Description = "One character at record start; omitted by default.")]
    public string? Comment { get; set; }

    /// <summary>Gets or sets the explicit Unicode encoding.</summary>
    [JsonPropertyName("encoding")]
    [CodeGenProperty("Dialect", Title = "Explicit encoding.", Options = ["Utf8", "Utf16LE", "Utf16BE", "Utf32LE", "Utf32BE"], Description = "Defaults to Utf8. Strict runtime decoding; no BOM-based encoding switching.")]
    public string? Encoding { get; set; }

    /// <summary>Gets or sets the flat named-header policy.</summary>
    [JsonPropertyName("headerPolicy")]
    [CodeGenProperty("Dialect", Title = "Named header policy.", Options = ["AllowAdditionalColumns", "Exact"], Description = "Flat named-column mappings only; defaults to AllowAdditionalColumns. Exact rejects extra header columns and rows whose field count differs from the header.")]
    public string? HeaderPolicy { get; set; }

    /// <summary>Gets or sets the discriminator field index.</summary>
    [JsonPropertyName("discriminatorIndex")]
    [CodeGenProperty("Routing", Title = "Zero-based discriminator index.", Description = "Selects unordered routing without structure; required with structure; omitted for flat input.")]
    public int? DiscriminatorIndex { get; set; }

    /// <summary>Gets or sets the per-root record budget.</summary>
    [JsonPropertyName("maxGroupRecords")]
    [CodeGenProperty("Routing", Title = "Positive records-per-root budget.", Description = "Defaults to 10000, including the root.")]
    public long? MaxGroupRecords { get; set; }

    /// <summary>Gets or sets the optional business-root hierarchy.</summary>
    [JsonPropertyName("structure")]
    [CodeGenProperty("Routing", Title = "Business-root hierarchy.", Description = "Omit for unordered routed records (with discriminatorIndex) or one flat record (without discriminatorIndex).")]
    public StructureConfig? Structure { get; set; }

    /// <summary>Gets the configured encoding expression.</summary>
    public string EncodingExpression => Encoding switch
    {
        "Utf8" => "new UTF8Encoding(false, true)",
        "Utf16LE" => "new UnicodeEncoding(false, false, true)",
        "Utf16BE" => "new UnicodeEncoding(true, false, true)",
        "Utf32LE" => "new UTF32Encoding(false, false, true)",
        "Utf32BE" => "new UTF32Encoding(true, false, true)",
        _ => throw new InvalidOperationException("Unprepared encoding.")
    };

    /// <summary>Gets safe dialect literals; indexing produces a char without ad-hoc escaping.</summary>
    public string DelimiterExpression => OnRamp.Utility.CSharpSource.StringLiteral(Delimiter!) + "[0]";

    /// <summary>Gets the safe quote expression.</summary>
    public string QuoteExpression => OnRamp.Utility.CSharpSource.StringLiteral(Quote!) + "[0]";

    /// <summary>Gets the optional safe comment expression.</summary>
    public string CommentExpression => Comment is null ? "null" : OnRamp.Utility.CSharpSource.StringLiteral(Comment) + "[0]";

    /// <summary>Gets the configured named-header policy expression.</summary>
    public string HeaderPolicyExpression => "CsvHeaderPolicy." + HeaderPolicy;

    /// <summary>Gets whether exact header validation is enabled for generated flat mappings.</summary>
    public bool IsExactHeaderPolicy => HeaderPolicy == "Exact";

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        HeaderPolicy = DefaultWhereNull(HeaderPolicy, () => "AllowAdditionalColumns");
        Encoding = DefaultWhereNull(Encoding, () => "Utf8");
        Delimiter = DefaultWhereNull(Delimiter, () => ",");
        Quote = DefaultWhereNull(Quote, () => "\"");
        MaxGroupRecords = DefaultWhereNull(MaxGroupRecords, () => 10_000);
        if (MaxGroupRecords <= 0)
            throw new CodeGenException(this, nameof(MaxGroupRecords), "The group-record limit must be positive.");

        if (new[] { Delimiter, Quote, Comment }.OfType<string>().Any(x => x.Length != 1 || x[0] is '\r' or '\n' or '\0' || char.IsSurrogate(x[0]))
            || Delimiter == Quote || Comment is not null && (Comment == Delimiter || Comment == Quote))
            throw new CodeGenException(this, nameof(Delimiter), "Dialect markers must be distinct single non-newline, non-null characters.");

        if (HeaderPolicy is not ("AllowAdditionalColumns" or "Exact"))
            throw new CodeGenException(this, nameof(HeaderPolicy), "The named header policy must be AllowAdditionalColumns or Exact.");

        if (Encoding is not ("Utf8" or "Utf16LE" or "Utf16BE" or "Utf32LE" or "Utf32BE"))
            throw new CodeGenException(this, nameof(Encoding), "Unsupported CSV encoding.");

        return Task.CompletedTask;
    }
}
