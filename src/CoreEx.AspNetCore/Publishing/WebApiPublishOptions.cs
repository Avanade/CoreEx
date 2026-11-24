namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Creates options for Web API message publishing.
/// </summary>
public static class WebApiPublishOptions
{
    /// <summary>
    /// Creates options for asynchronous payload creation without parsing a request body.
    /// </summary>
    /// <typeparam name="TMessage">The message payload type.</typeparam>
    /// <param name="function">The function that creates the payload or returns a failure; a thrown exception is handled by the standard Web API error policy.</param>
    /// <returns>The custom publishing options.</returns>
    public static WebApiPublishCustomOptions CreateWithValueFactory<TMessage>(Func<WebApiOptions, CancellationToken, Task<Result<TMessage>>> function)
    {
        function.ThrowIfNull();
        return new WebApiPublishCustomOptions(typeof(TMessage), async (options, cancellationToken) =>
        {
            var result = await function(options, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? Result<EventData>.Fail(result.Error) : Result<EventData>.Ok(WebApiPublishOptionsBase.CreateDefaultMessage(result.Value));
        });
    }

    /// <summary>
    /// Creates options for asynchronous creation of a complete event envelope without parsing a request body.
    /// </summary>
    /// <typeparam name="TMessage">The declared message payload type.</typeparam>
    /// <param name="function">The function that creates the event envelope or returns a failure; a thrown exception is handled by the standard Web API error policy.</param>
    /// <returns>The custom publishing options.</returns>
    /// <remarks>For tracked publishing, <typeparamref name="TMessage"/> must match the payload type in the returned envelope.</remarks>
    public static WebApiPublishCustomOptions CreateWithEventFactory<TMessage>(Func<WebApiOptions, CancellationToken, Task<Result<EventData>>> function) => new(typeof(TMessage), function.ThrowIfNull());

    /// <summary>
    /// Creates options that parse and publish the request body as the message payload.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request and message payload type.</typeparam>
    /// <returns>The typed publishing options.</returns>
    public static WebApiPublishOptions<TRequest> Create<TRequest>() => new();

    /// <summary>
    /// Creates options that publish an already-deserialized request value.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request and message payload type.</typeparam>
    /// <param name="value">The request value.</param>
    /// <returns>The typed publishing options.</returns>
    public static WebApiPublishOptions<TRequest> Create<TRequest>(TRequest value) => new WebApiPublishOptions<TRequest>().WithValue(value);

    /// <summary>
    /// Creates options that parse, map and publish the request body.
    /// </summary>
    /// <typeparam name="TRequest">The HTTP request type.</typeparam>
    /// <typeparam name="TMessage">The published message payload type.</typeparam>
    /// <param name="mapper">The request-to-message mapper.</param>
    /// <returns>The mapped publishing options.</returns>
    public static WebApiPublishOptions<TRequest, TMessage> CreateMapped<TRequest, TMessage>(Mapping.IMapper<TRequest, TMessage> mapper) where TRequest : class where TMessage : class => new(mapper.ThrowIfNull());
}
