namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines format-neutral file-processing budgets.</summary>
[CodeGenClass("Limits", Title = "Shared file-processing limits.")]
[CodeGenCategory("Primary", Title = "Format-neutral operating budgets.")]
public class LimitsConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the maximum source bytes consumed per pass.</summary>
    [JsonPropertyName("maxInputBytes")]
    [CodeGenProperty("Primary", Title = "Positive input-byte budget.", Description = "Defaults to 67108864 (64 MiB).")]
    public long? MaxInputBytes { get; set; }

    /// <summary>Gets or sets the maximum candidate business units.</summary>
    [JsonPropertyName("maxUnits")]
    [CodeGenProperty("Primary", Title = "Positive business-unit budget.", Description = "Defaults to 100000.")]
    public long? MaxUnits { get; set; }

    /// <summary>Gets or sets the maximum ordinary diagnostics.</summary>
    [JsonPropertyName("maxDiagnostics")]
    [CodeGenProperty("Primary", Title = "Positive diagnostic budget.", Description = "Defaults to 1000; one terminal limit diagnostic may follow.")]
    public long? MaxDiagnostics { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        MaxInputBytes = DefaultWhereNull(MaxInputBytes, () => 64 * 1024 * 1024);
        MaxUnits = DefaultWhereNull(MaxUnits, () => 100_000);
        MaxDiagnostics = DefaultWhereNull(MaxDiagnostics, () => 1_000);
        if (MaxInputBytes <= 0 || MaxUnits <= 0 || MaxDiagnostics <= 0)
            throw new CodeGenException(this, nameof(MaxInputBytes), "All shared limits must be positive.");

        return Task.CompletedTask;
    }
}
