namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines CSV-specific routing for one record type.</summary>
[CodeGenClass("CsvRecord", Title = "CSV record routing.")]
[CodeGenCategory("Primary", Title = "Discriminator and record occurrences.")]
public class CsvRecordConfig : ConfigBase<CodeGenConfig, RecordConfig>
{
    /// <summary>Gets or sets the exact discriminator value.</summary>
    [JsonPropertyName("match")]
    [CodeGenProperty("Primary", Title = "Exact discriminator.", Description = "Required for routed files; forbidden for flat files.")]
    public string? Match { get; set; }

    /// <summary>Gets or sets the unordered route's recognized occurrences per file.</summary>
    [JsonPropertyName("cardinality")]
    [CodeGenProperty("Primary", Title = "File cardinality.", Options = ["Many", "One", "Optional", "OneOrMore"], Description = "Unordered routed files only; defaults to Many. Counts include recognized invalid records. Hierarchical cardinality belongs on structure nodes.")]
    public string? Cardinality { get; set; }

    /// <summary>Gets or sets the unordered route's optional file-boundary restriction.</summary>
    [JsonPropertyName("position")]
    [CodeGenProperty("Primary", Title = "File position.", Options = ["Any", "First", "Last"], Description = "Unordered routed files only; defaults to Any. First/Last require One or Optional cardinality.")]
    public string? Position { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (Root!.IsUnordered)
        {
            Cardinality = DefaultWhereNull(Cardinality, () => "Many");
            Position = DefaultWhereNull(Position, () => "Any");
            if (Position != "Any" && Cardinality is not ("One" or "Optional"))
                throw new CodeGenException(this, nameof(Position), "First and Last positions require One or Optional cardinality.");
        }
        else if (Cardinality is not null || Position is not null)
            throw new CodeGenException(this, nameof(Cardinality), "Record cardinality/position are only valid for unordered routed files.");

        return Task.CompletedTask;
    }
}
