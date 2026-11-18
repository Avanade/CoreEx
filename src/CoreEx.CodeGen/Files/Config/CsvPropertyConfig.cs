namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines the CSV-specific positional selector for one source field.</summary>
[CodeGenClass("CsvProperty", Title = "CSV property selector.")]
[CodeGenCategory("Primary", Title = "Source field selection.")]
public class CsvPropertyConfig : ConfigBase<CodeGenConfig, PropertyConfig>
{
    /// <summary>Gets or sets the zero-based positional field index.</summary>
    [JsonPropertyName("index")]
    [CodeGenProperty("Primary", Title = "Zero-based CSV index.", Description = "Required for routed files; flat files may use a named source field instead.")]
    public int? Index { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (Index < 0)
            throw new CodeGenException(this, nameof(Index), "The CSV index must be nonnegative.");

        if (Root!.IsRouted && !Index.HasValue)
            throw new CodeGenException(this, nameof(Index), "Routed CSV fields require a positional index.");

        return Task.CompletedTask;
    }
}
