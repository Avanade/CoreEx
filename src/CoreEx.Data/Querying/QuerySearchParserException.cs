namespace CoreEx.Data.Querying;

/// <summary>
/// Represents a <see cref="QuerySearchParser"/> <see cref="ValidationException"/>.
/// </summary>
/// <param name="message">The error message.</param>
public sealed class QuerySearchParserException(string message) : ValidationException(new MessageItem(MessageType.Error, message, HttpNames.QuerySearchQueryStringName), FallbackMessage)
{
    /// <summary>
    /// Gets the default/fallback <see cref="Exception.Message"/>
    /// </summary>
    internal const string FallbackMessage = "A query search parsing error occurred.";
}
