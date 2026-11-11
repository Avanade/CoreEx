namespace CoreEx.IO;

/// <summary>Defines occurrence counts for nested collections and repeated units.</summary>
public enum FileCardinality
{
    /// <summary>Allows zero or more occurrences.</summary>
    Many,

    /// <summary>Requires exactly one occurrence.</summary>
    One,

    /// <summary>Allows zero or one occurrence.</summary>
    Optional,

    /// <summary>Requires one or more occurrences.</summary>
    OneOrMore
}
