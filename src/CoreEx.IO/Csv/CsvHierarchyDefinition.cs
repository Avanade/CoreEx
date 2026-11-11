namespace CoreEx.IO.Csv;

/// <summary>Declares one business-root hierarchy and optional file-level metadata with exact discriminator routing.</summary>
/// <typeparam name="TGroup">The projected business unit.</typeparam>
public sealed class CsvHierarchyDefinition<TGroup> where TGroup : class
{
    /// <summary>Initializes and validates a complete immutable hierarchy definition.</summary>
    /// <param name="discriminatorIndex">The zero-based discriminator field index.</param>
    /// <param name="routes">Typed routes, each used exactly once in the declared structure.</param>
    /// <param name="root">The business-root record and ordered descendants.</param>
    /// <param name="project">Projects one accepted root subtree into a typed business unit.</param>
    /// <param name="metadata">Optional unique file-level metadata declarations.</param>
    public CsvHierarchyDefinition(int discriminatorIndex, IEnumerable<CsvRecordRoute> routes, CsvHierarchyNode root, Func<CsvAggregateNode, TGroup> project,
        IEnumerable<CsvMetadataDefinition>? metadata = null)
    {
        DiscriminatorIndex = discriminatorIndex.ThrowIfLessThanZero();
        routes.ThrowIfNull();
        Root = root.ThrowIfNull();
        Project = project.ThrowIfNull();
        var list = routes.ToArray();
        if (list.Length == 0)
            throw new ArgumentException("At least one route is required.", nameof(routes));

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in list)
        {
            route.ThrowIfNull();
            if (!keys.Add(route.Key) || !matches.Add(route.Match))
                throw new ArgumentException("Route keys must be unique ignoring case and discriminator matches must be exactly unique.", nameof(routes));
        }

        var byKey = list.ToDictionary(x => x.Key, StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        void Check(CsvHierarchyNode node)
        {
            if (!byKey.ContainsKey(node.Key) || !used.Add(node.Key))
                throw new ArgumentException("Every hierarchy key must reference a unique configured route.", nameof(root));

            foreach (var child in node.Children)
                Check(child);
        }

        Check(root);
        var metadataList = (metadata ?? []).Select(x => x.Validate()).ToArray();
        var metadataNames = new HashSet<string>(StringComparer.Ordinal);
        var metadataKeys = new HashSet<string>(StringComparer.Ordinal);
        var firstCount = 0;
        var lastCount = 0;

        foreach (var item in metadataList)
        {
            if (!metadataNames.Add(item.Name) || !metadataKeys.Add(item.RecordKey) || !byKey.ContainsKey(item.RecordKey))
                throw new ArgumentException("Metadata names and route keys must be unique, and every metadata route must exist.", nameof(metadata));

            firstCount += item.Position == FileRecordPosition.First ? 1 : 0;
            lastCount += item.Position == FileRecordPosition.Last ? 1 : 0;
        }

        if (firstCount > 1 || lastCount > 1)
            throw new ArgumentException("Only one metadata route may be restricted to First and one to Last.", nameof(metadata));

        if (metadataKeys.Any(used.Contains) || used.Count + metadataKeys.Count != list.Length)
            throw new ArgumentException("Every configured route must belong to the hierarchy.", nameof(routes));

        Routes = Array.AsReadOnly(list);
        Metadata = Array.AsReadOnly(metadataList);
    }

    /// <summary>Gets the discriminator index.</summary>
    public int DiscriminatorIndex { get; }

    /// <summary>Gets the validated route table.</summary>
    public IReadOnlyList<CsvRecordRoute> Routes { get; }

    /// <summary>Gets the business-root structure.</summary>
    public CsvHierarchyNode Root { get; }

    /// <summary>Gets the declared file-level metadata routes.</summary>
    public IReadOnlyList<CsvMetadataDefinition> Metadata { get; }

    /// <summary>Gets the typed group projection.</summary>
    internal Func<CsvAggregateNode, TGroup> Project { get; }
}
