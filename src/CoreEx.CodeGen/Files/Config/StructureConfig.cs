namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one business-root hierarchy.</summary>
[CodeGenClass("Structure", Title = "Hierarchical file structure.")]
[CodeGenCategory("Structure", Title = "Business root and descendants.")]
public class StructureConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets the owning definition without exposing its runtime root as YAML data.</summary>
    [JsonIgnore]
    public new CodeGenConfig? Root => base.Root;

    /// <summary>Gets or sets the root node.</summary>
    [JsonPropertyName("root")]
    [CodeGenProperty("Structure", Title = "Business root.", IsMandatory = true)]
    public NodeConfig? RootNode { get; set; }

    /// <summary>Gets all business nodes in pre-order.</summary>
    public IEnumerable<NodeConfig> Nodes => Flatten(RootNode!);

    /// <summary>Traverses only declared descendants.</summary>
    private static IEnumerable<NodeConfig> Flatten(NodeConfig node) => new[] { node }.Concat((node.Children ?? []).SelectMany(Flatten));

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (RootNode is null)
            throw new CodeGenException(this, nameof(RootNode), "A business root is required.");

        await RootNode.PrepareAsync(Root!, this).ConfigureAwait(false);
        var used = Nodes.Select(x => x.Record).ToArray();
        if (used.Distinct(StringComparer.Ordinal).Count() != used.Length)
            throw new CodeGenException(this, nameof(RootNode), "Each business record must appear only once in the hierarchy.");
    }
}
