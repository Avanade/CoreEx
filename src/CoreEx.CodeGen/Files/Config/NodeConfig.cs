namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one contiguous hierarchy position.</summary>
[CodeGenClass("Node", Title = "Hierarchy position.")]
[CodeGenCategory("Structure", Title = "Record ownership and occurrences.")]
[CodeGenCategory("Collections", Title = "Ordered descendants.")]
public class NodeConfig : ConfigBase<CodeGenConfig, ConfigBase>
{
    /// <summary>Gets or sets the exact logical record name.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Structure", Title = "Record name.", IsMandatory = true)]
    public string? Record { get; set; }

    /// <summary>Gets or sets occurrences per parent or file.</summary>
    [JsonPropertyName("cardinality")]
    [CodeGenProperty("Structure", Title = "Occurrences.", Options = ["Many", "One", "Optional", "OneOrMore"], Description = "Defaults to Many.")]
    public string? Cardinality { get; set; }

    /// <summary>Gets or sets ordered contiguous children.</summary>
    [JsonPropertyName("children")]
    [CodeGenPropertyCollection("Collections", Title = "Ordered children.")]
    public List<NodeConfig>? Children { get; set; }

    /// <summary>Gets the matching wire record configuration.</summary>
    public RecordConfig RecordConfig => Root!.Records!.Single(x => x.Name == Record);

    /// <summary>Gets whether this position needs an assembled representation.</summary>
    public bool HasChildren => Children?.Count > 0;

    /// <summary>Gets the assembled or leaf type name.</summary>
    public string TypeName => HasChildren ? Root!.Name + Record + "Group" : RecordConfig.TypeName;

    /// <summary>Gets the emitted hierarchy expression.</summary>
    public string DefinitionExpression => $"new CsvHierarchyNode({OnRamp.Utility.CSharpSource.StringLiteral(Record!)}, CsvRecordCardinality.{Cardinality}" +
        string.Concat((Children ?? []).Select(x => ", " + x.DefinitionExpression)) + ")";

    /// <summary>Gets the projection expression for this node.</summary>
    public string ProjectionExpression => HasChildren ? $"Project{Record}(node)" : $"node.GetRecord<{RecordConfig.TypeName}>()";

    /// <summary>Gets a safe record key.</summary>
    public string KeyLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Record!);

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        ConfigValidation.Identifier(this, nameof(Record), Record);
        Cardinality = DefaultWhereNull(Cardinality, () => "Many");
        if (!Root!.Records!.Any(x => x.Name == Record))
            throw new CodeGenException(this, nameof(Record), "The node must reference a declared record exactly.");

        if (Children?.Any(x => x is null) == true)
            throw new CodeGenException(this, nameof(Children), "Null child definitions are not supported.");

        Children = await PrepareCollectionAsync(Children).ConfigureAwait(false);
        if (Children?.Any(x => x.Record is "Record" or "LineNumber" || x.Record == TypeName || typeof(object).GetMembers().Any(m => m.Name == x.Record)) == true)
            throw new CodeGenException(this, nameof(Children), "Child property conflicts with the generated group.");
    }
}
