namespace CoreEx.IO.Csv;

/// <summary>Defines recognized occurrences per file for a root/envelope, or per owning parent for a child.</summary>
public enum CsvRecordCardinality
{
    /// <summary>Zero or more occurrences.</summary>
    Many,

    /// <summary>Exactly one occurrence.</summary>
    One,

    /// <summary>Zero or one occurrence.</summary>
    Optional,

    /// <summary>One or more occurrences.</summary>
    OneOrMore
}
