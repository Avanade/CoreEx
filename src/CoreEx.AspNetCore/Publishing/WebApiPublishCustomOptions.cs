namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures custom asynchronous message preparation for Web API publishing.
/// </summary>
public sealed class WebApiPublishCustomOptions : WebApiPublishOptionsBase<WebApiPublishCustomOptions>
{
    private readonly Type _publishedType;
    private readonly Func<WebApiOptions, CancellationToken, Task<Result<EventData>>> _prepare;
    private Func<WebApiOptions, CompositeKey>? _operationKeyFactory;

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

    /// <summary>
    /// Derives a repeatable submission key from the current Web API request.
    /// </summary>
    /// <param name="keyFactory">The request-aware submission-key selector.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishCustomOptions WithOperationKey(Func<WebApiOptions, CompositeKey> keyFactory)
    {
        _operationKeyFactory = keyFactory.ThrowIfNull();
        return this;
    }

    /// <inheritdoc/>
    internal override async Task<Result<WebApiPreparedPublish>> PrepareAsync<TResult>(WebApi<TResult> webApi, WebApiOptions requestOptions, CancellationToken cancellationToken)
    {
        var result = await _prepare(requestOptions, cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? result.AsResult<EventData, WebApiPreparedPublish>() : Result.Ok(new WebApiPreparedPublish(result.Value, _operationKeyFactory?.Invoke(requestOptions)));
    }
}
