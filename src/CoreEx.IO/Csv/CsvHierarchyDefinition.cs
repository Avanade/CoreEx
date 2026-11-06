namespace CoreEx.IO.Csv;

/// <summary>Declares one business-root hierarchy and optional first/last envelopes with exact discriminator routing.</summary>
/// <typeparam name="TGroup">The projected business unit.</typeparam>
public sealed class CsvHierarchyDefinition<TGroup> where TGroup : class
{
    /// <summary>Initializes and validates a complete immutable hierarchy definition.</summary>
    /// <param name="discriminatorIndex">The zero-based discriminator field index.</param>
    /// <param name="routes">Typed routes, each used exactly once in the declared structure.</param>
    /// <param name="root">The business-root record and ordered descendants.</param>
    /// <param name="project">Projects one accepted root subtree into a typed business unit.</param>
    /// <param name="header">Optional first-record envelope with One or Optional cardinality and no children.</param>
    /// <param name="trailer">Optional last-record envelope with One or Optional cardinality and no children.</param>
    public CsvHierarchyDefinition(int discriminatorIndex, IEnumerable<CsvRecordRoute> routes, CsvHierarchyNode root, Func<CsvAggregateNode, TGroup> project,
        CsvHierarchyNode? header = null, CsvHierarchyNode? trailer = null)
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
        foreach (var envelope in new[] { header, trailer }.OfType<CsvHierarchyNode>())
        {
            if (envelope.Children.Count != 0 || envelope.Cardinality is not (CsvRecordCardinality.One or CsvRecordCardinality.Optional))
                throw new ArgumentException("File envelopes require One or Optional cardinality and no children.", nameof(header));

            Check(envelope);
        }

        if (used.Count != list.Length)
            throw new ArgumentException("Every configured route must belong to the hierarchy.", nameof(routes));

        Routes = Array.AsReadOnly(list);
        Header = header;
        Trailer = trailer;
    }

    /// <summary>Gets the discriminator index.</summary>
    public int DiscriminatorIndex { get; }

    /// <summary>Gets the validated route table.</summary>
    public IReadOnlyList<CsvRecordRoute> Routes { get; }

    /// <summary>Gets the business-root structure.</summary>
    public CsvHierarchyNode Root { get; }

    /// <summary>Gets the optional first envelope.</summary>
    public CsvHierarchyNode? Header { get; }

    /// <summary>Gets the optional last envelope.</summary>
    public CsvHierarchyNode? Trailer { get; }

    /// <summary>Gets the typed group projection.</summary>
    internal Func<CsvAggregateNode, TGroup> Project { get; }
}
