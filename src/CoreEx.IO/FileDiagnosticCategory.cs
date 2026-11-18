namespace CoreEx.IO;

/// <summary>Classifies expected content diagnostics independently of file format.</summary>
public enum FileDiagnosticCategory
{
    /// <summary>The input could not be parsed into a physical record.</summary>
    Parsing,

    /// <summary>A physical field could not be mapped to its declared type.</summary>
    Mapping,

    /// <summary>A record violated ordering, cardinality, or parentage requirements.</summary>
    Structure,

    /// <summary>A typed record or business group violated a validation rule.</summary>
    Validation,

    /// <summary>File-wide counts, totals, or other reconciliation rules failed.</summary>
    Reconciliation
}
