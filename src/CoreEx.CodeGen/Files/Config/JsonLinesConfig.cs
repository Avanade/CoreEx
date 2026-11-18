namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines JSONL-only framing and opt-in metadata routing.</summary>
[CodeGenClass("Jsonl", Title = "JSONL framing and routing.")]
[CodeGenCategory("Primary", Title = "Line framing and explicit discriminator.")]
public class JsonLinesConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the exact direct string member used for routing.</summary>
    [JsonPropertyName("discriminator")]
    [CodeGenProperty("Primary", Title = "Direct string discriminator.", Description = "Omit for ordinary object units. Required for metadata routing; no implicit type-member convention.")]
    public string? Discriminator { get; set; }

    /// <summary>Gets or sets the blank-line policy.</summary>
    [JsonPropertyName("blankLinePolicy")]
    [CodeGenProperty("Primary", Title = "Blank-line policy.", Options = ["Reject", "Skip"], Description = "Defaults to Reject. Skip excludes blank lines from records/units but not bytes/physical lines.")]
    public string? BlankLinePolicy { get; set; }

    /// <summary>Gets the exact discriminator literal, or null.</summary>
    public string DiscriminatorLiteral => Discriminator is null ? "null" : OnRamp.Utility.CSharpSource.StringLiteral(Discriminator);

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (Discriminator is not null)
            RequireText(nameof(Discriminator), Discriminator);

        BlankLinePolicy ??= "Reject";
        if (BlankLinePolicy is not ("Reject" or "Skip"))
            throw new CodeGenException(this, nameof(BlankLinePolicy), "Require Reject or Skip.");

        return Task.CompletedTask;
    }
}
