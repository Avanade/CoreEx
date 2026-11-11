namespace CoreEx.IO.Csv;

/// <summary>Declares a record's cardinality and contiguous children in required sibling order.</summary>
public sealed class CsvHierarchyNode
{
    /// <summary>Initializes an immutable hierarchy position.</summary>
    /// <param name="key">The matching logical route key.</param>
    /// <param name="cardinality">Occurrences within the containing file or parent.</param>
    /// <param name="children">Children in declared order; each subtree must remain contiguous.</param>
    public CsvHierarchyNode(string key, FileCardinality cardinality = FileCardinality.Many, params CsvHierarchyNode[] children)
    {
        Key = key.ThrowIfNullOrWhiteSpace();
        if (!Enum.IsDefined(cardinality))
            throw new ArgumentOutOfRangeException(nameof(cardinality));

        children.ThrowIfNull();
        foreach (var child in children)
            child.ThrowIfNull();

        Cardinality = cardinality;
        Children = Array.AsReadOnly(children.ToArray());
    }

    /// <summary>Gets the logical route key.</summary>
    public string Key { get; }

    /// <summary>Gets the permitted occurrence count.</summary>
    public FileCardinality Cardinality { get; }

    /// <summary>Gets children in declared contiguous order.</summary>
    public IReadOnlyList<CsvHierarchyNode> Children { get; }
}
