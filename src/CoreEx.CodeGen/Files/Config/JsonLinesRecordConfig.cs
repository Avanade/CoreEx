namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines one explicit JSONL route match, independent of nested type occurrences.</summary>
[CodeGenClass("JsonlRecord", Title = "JSONL record route.")]
[CodeGenCategory("Primary", Title = "Exact discriminator match.")]
public class JsonLinesRecordConfig : ConfigBase<CodeGenConfig, RecordConfig>
{
    /// <summary>Gets or sets the case-sensitive string match.</summary>
    [JsonPropertyName("match")]
    [CodeGenProperty("Primary", Title = "Exact discriminator match.", IsMandatory = true)]
    public string? Match { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Match = RequireText(nameof(Match), Match);
        return Task.CompletedTask;
    }
}
