namespace CoreEx.Data.Querying;

/// <summary>
/// Represents a <see cref="QueryFilterParser"/> <see cref="ValidationException"/>.
/// </summary>
/// <param name="message">The error message.</param>
public sealed class QueryFilterParserException(string message) : ValidationException(new MessageItem(MessageType.Error, message, HttpNames.QueryFilterQueryStringName), FallbackMessage)
{
    /// <summary>
    /// Gets the default/fallback <see cref="Exception.Message"/>
    /// </summary>
    internal const string FallbackMessage = "A query filter parsing error occurred.";
}