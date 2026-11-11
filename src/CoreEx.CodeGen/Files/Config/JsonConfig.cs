namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines shared JSON mapping and syntax settings; document readers are not yet supported.</summary>
[CodeGenClass("Json", Title = "Shared JSON mapping settings.")]
[CodeGenCategory("Primary", Title = "Member handling and parser depth.")]
public class JsonConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the unknown-member policy.</summary>
    [JsonPropertyName("unknownMemberPolicy")]
    [CodeGenProperty("Primary", Title = "Unknown member policy.", Options = ["Reject", "Ignore"], Description = "Defaults to Reject; applies recursively to units and declared metadata.")]
    public string? UnknownMemberPolicy { get; set; }

    /// <summary>Gets or sets the maximum nesting depth.</summary>
    [JsonPropertyName("maxDepth")]
    [CodeGenProperty("Primary", Title = "Positive nesting limit.", Description = "Defaults to 64; includes ignored objects and arrays.")]
    public int? MaxDepth { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        UnknownMemberPolicy ??= "Reject";
        MaxDepth ??= 64;
        if (UnknownMemberPolicy is not ("Reject" or "Ignore") || MaxDepth <= 0)
            throw new CodeGenException(this, nameof(UnknownMemberPolicy), "Require Reject/Ignore and a positive JSON nesting depth.");

        return Task.CompletedTask;
    }
}
