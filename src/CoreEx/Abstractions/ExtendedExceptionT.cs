namespace CoreEx.Abstractions;

/// <summary>
/// Provides the base <see cref="IExtendedException"/> implementation.
/// </summary>
/// <typeparam name="TSelf">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/> itself.</typeparam>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner <see cref="Exception"/>.</param>
/// <param name="defaultLoggingEnablement">The default logging enablement where no corresponding configuration setting is found.</param>
public abstract class ExtendedException<TSelf>(LText? message, Exception? innerException, bool defaultLoggingEnablement = false)
    : ExtendedException(message, innerException, typeof(TSelf), defaultLoggingEnablement) where TSelf : ExtendedException<TSelf>
{
    /// <summary>
    /// Gets the configured <see cref="HttpStatusCode"/> for the exception type.
    /// </summary>
    /// <param name="default">The default where not configured.</param>
    /// <param name="configuration">The optional <see cref="IConfiguration"/>; otherwise, defaults from <see cref="ExecutionContext"/>.</param>
    /// <returns>The <see cref="HttpStatusCode"/>.</returns>
    protected HttpStatusCode GetConfiguredStatusCode(HttpStatusCode @default, IConfiguration? configuration = null)
        => Internal.GetConfigurationValue<HttpStatusCode>($"CoreEx:Exception:{typeof(TSelf).Name}:StatusCode", @default, configuration);
}