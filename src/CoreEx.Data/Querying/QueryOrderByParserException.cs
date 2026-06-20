namespace CoreEx.Data.Querying;

/// <summary>
/// Represents a <see cref="QueryOrderByParser"/> <see cref="ValidationException"/>.
/// </summary>
/// <param name="message">The error message.</param>
public sealed class QueryOrderByParserException(string message) : ValidationException(new MessageItem(MessageType.Error, message, HttpNames.QueryOrderByQueryStringName), FallbackMessage)
{
    /// <summary>
    /// Gets the default/fallback <see cref="Exception.Message"/>
    /// </summary>
    internal const string FallbackMessage = "A query order-by parsing error occurred.";
}