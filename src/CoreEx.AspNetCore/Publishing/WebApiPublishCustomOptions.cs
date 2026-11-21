namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures custom asynchronous message preparation for Web API publishing.
/// </summary>
public sealed class WebApiPublishCustomOptions : WebApiPublishOptionsBase<WebApiPublishCustomOptions>
{
    private readonly Type _publishedType;
    private readonly Func<WebApiOptions, CancellationToken, Task<Result<EventData>>> _prepare;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiPublishCustomOptions"/> class.
    /// </summary>
    /// <param name="publishedType">The declared message payload type.</param>
    /// <param name="prepare">The custom message preparation function.</param>
    internal WebApiPublishCustomOptions(Type publishedType, Func<WebApiOptions, CancellationToken, Task<Result<EventData>>> prepare)
    {
        _publishedType = publishedType.ThrowIfNull();
        _prepare = prepare.ThrowIfNull();
    }

    /// <summary>
    /// Gets the declared message payload type.
    /// </summary>
    internal override Type PublishedType => _publishedType;

    /// <inheritdoc/>
    internal override Task<Result<EventData>> PrepareAsync<TResult>(WebApi<TResult> webApi, WebApiOptions requestOptions, CancellationToken cancellationToken) => _prepare(requestOptions, cancellationToken);
}
