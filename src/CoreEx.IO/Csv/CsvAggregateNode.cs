namespace CoreEx.IO.Csv;

/// <summary>Contains one structurally accepted subtree for typed group projection, with explicit physical provenance.</summary>
public sealed class CsvAggregateNode
{
    private readonly object _record;

    /// <summary>Initializes a completed accepted subtree.</summary>
    internal CsvAggregateNode(string key, object record, long line, IReadOnlyList<CsvAggregateNode> children)
        => (Key, _record, LineNumber, Children) = (key, record, line, children);

    /// <summary>Gets the logical route key.</summary>
    public string Key { get; }

    /// <summary>Gets the physical starting line.</summary>
    public long LineNumber { get; }

    /// <summary>Gets accepted children in source order.</summary>
    public IReadOnlyList<CsvAggregateNode> Children { get; }

    /// <summary>Gets the typed wire record, failing explicitly for an incorrect projection.</summary>
    /// <typeparam name="TRecord">The declared route record type.</typeparam>
    /// <returns>The typed record.</returns>
    public TRecord GetRecord<TRecord>() where TRecord : class => _record as TRecord
        ?? throw new InvalidOperationException("The hierarchy projection requested an unexpected record type.");

    /// <summary>Projects child occurrences of a logical key in input order.</summary>
    /// <typeparam name="TChild">The projected child type.</typeparam>
    /// <param name="key">The child route key.</param>
    /// <param name="project">The typed projection.</param>
    /// <returns>The current subtree's projected children.</returns>
    public IReadOnlyList<TChild> GetChildren<TChild>(string key, Func<CsvAggregateNode, TChild> project)
    {
        key.ThrowIfNullOrWhiteSpace();
        project.ThrowIfNull();
        return Array.AsReadOnly(Children.Where(x => x.Key == key).Select(project).ToArray());
    }

    /// <summary>Finds an owned record's physical line without interpreting a logical property path.</summary>
    /// <param name="record">The exact wire-record instance in this subtree.</param>
    /// <returns>The physical starting line.</returns>
    public long GetLineNumber(object record) => FindLine(record.ThrowIfNull())
        ?? throw new ArgumentException("The record does not belong to this group.", nameof(record));

    /// <summary>Searches only the current subtree by reference identity.</summary>
    private long? FindLine(object record) => ReferenceEquals(record, _record) ? LineNumber : Children.Select(x => x.FindLine(record)).FirstOrDefault(x => x.HasValue);
}
