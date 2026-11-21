namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures validation and message creation for a typed Web API request.
/// </summary>
/// <typeparam name="TRequest">The HTTP request and message payload type.</typeparam>
public sealed class WebApiPublishOptions<TRequest> : WebApiPublishOptionsBase<WebApiPublishOptions<TRequest>>
{
    private TRequest? _value;
    private bool _hasValue;
    private Func<TRequest, EventData>? _messageFactory;

    /// <summary>
    /// Gets the optional validator.
    /// </summary>
    public IValidator<TRequest>? Validator { get; private set; }

    /// <summary>
    /// Gets the published message payload type.
    /// </summary>
    internal override Type PublishedType => typeof(TRequest);

    /// <summary>
    /// Sets the request value to publish without parsing the HTTP request body.
    /// </summary>
    /// <param name="value">The request value.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest> WithValue(TRequest? value)
    {
        _value = value;
        _hasValue = true;
        return this;
    }

    /// <summary>
    /// Sets the request validator.
    /// </summary>
    /// <param name="validator">The validator.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest> WithValidator(IValidator<TRequest> validator)
    {
        Validator = validator.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Sets the event factory for the request value.
    /// </summary>
    /// <param name="messageFactory">The function that creates the event envelope.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest> WithEventFactory(Func<TRequest, EventData> messageFactory)
    {
        _messageFactory = messageFactory.ThrowIfNull();
        return this;
    }

    /// <inheritdoc/>
    internal override async Task<Result<EventData>> PrepareAsync<TResult>(WebApi<TResult> webApi, WebApiOptions requestOptions, CancellationToken cancellationToken)
    {
        if (_hasValue)
            return await webApi.PreparePublishValueAsync(requestOptions, _value, Validator, CreateMessage, cancellationToken).ConfigureAwait(false);

        return await webApi.PreparePublishRequestAsync<TRequest>(requestOptions, Validator, CreateMessage, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an event envelope using the configured message factory or default settings.
    /// </summary>
    /// <param name="value">The request payload.</param>
    /// <returns>The event envelope.</returns>
    private EventData CreateMessage(TRequest value) => _messageFactory?.Invoke(value) ?? CreateDefaultMessage(value);
}
