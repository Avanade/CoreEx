namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one business-root hierarchy and optional first/last envelopes.</summary>
[CodeGenClass("Structure", Title = "Hierarchical file structure.")]
[CodeGenCategory("Structure", Title = "Root and file envelopes.")]
public class StructureConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets the owning definition without exposing its runtime root as YAML data.</summary>
    [JsonIgnore]
    public new CodeGenConfig? Root => base.Root;

    /// <summary>Gets or sets the root node.</summary>
    [JsonPropertyName("root")]
    [CodeGenProperty("Structure", Title = "Business root.", IsMandatory = true)]
    public NodeConfig? RootNode { get; set; }

    /// <summary>Gets or sets the optional first envelope.</summary>
    [JsonPropertyName("header")]
    [CodeGenProperty("Structure", Title = "First envelope.", Description = "One or Optional, without children.")]
    public NodeConfig? Header { get; set; }

    /// <summary>Gets or sets the optional final envelope.</summary>
    [JsonPropertyName("trailer")]
    [CodeGenProperty("Structure", Title = "Final envelope.", Description = "One or Optional, without children.")]
    public NodeConfig? Trailer { get; set; }

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
        foreach (var envelope in new[] { Header, Trailer }.OfType<NodeConfig>())
        {
            await envelope.PrepareAsync(Root!, this).ConfigureAwait(false);
            if (envelope.HasChildren || envelope.Cardinality is not ("One" or "Optional"))
                throw new CodeGenException(envelope, nameof(envelope.Cardinality), "Envelopes require One/Optional cardinality without children.");
        }

        var used = Nodes.Concat(new[] { Header, Trailer }.OfType<NodeConfig>()).Select(x => x.Record).ToArray();
        if (used.Distinct(StringComparer.Ordinal).Count() != used.Length || used.Length != Root!.Records!.Count)
            throw new CodeGenException(this, nameof(RootNode), "Use every declared record exactly once across the hierarchy and envelopes.");
    }
}
