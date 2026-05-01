namespace CoreEx.Data.Querying;

/// <summary>
/// Represents an <see cref="Error"/> that occurred during parsing.
/// </summary>
internal interface IQueryParseError
{
    /// <summary>
    /// Indicates whether there was an <see cref="Error"/> during parsing.
    /// </summary>
    bool HasError { get; }

    /// <summary>
    /// Gets the error represented as an <see cref="ExtendedException"/> that occurred during parsing, if any.
    /// </summary>
    ExtendedException? Error { get; }
}