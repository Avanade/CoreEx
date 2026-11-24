namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures validation, request mapping and message creation for a typed Web API request.
/// </summary>
/// <typeparam name="TRequest">The HTTP request type.</typeparam>
/// <typeparam name="TMessage">The published message payload type.</typeparam>
public sealed class WebApiPublishOptions<TRequest, TMessage> : WebApiPublishOptionsBase<WebApiPublishOptions<TRequest, TMessage>> where TRequest : class where TMessage : class
{
    private TRequest? _value;
    private bool _hasValue;
    private Func<TMessage, EventData>? _messageFactory;
    private Func<WebApiOptions, TRequest, CompositeKey>? _operationKeyFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiPublishOptions{TRequest, TMessage}"/> class.
    /// </summary>
    /// <param name="mapper">The request-to-message mapper.</param>
    internal WebApiPublishOptions(Mapping.IMapper<TRequest, TMessage> mapper) => Mapper = mapper;

    /// <summary>
    /// Gets the optional validator.
    /// </summary>
    public IValidator<TRequest>? Validator { get; private set; }

    /// <summary>
    /// Gets the request-to-message mapper.
    /// </summary>
    public Mapping.IMapper<TRequest, TMessage> Mapper { get; }

    /// <summary>
    /// Gets the published message payload type.
    /// </summary>
    internal override Type PublishedType => typeof(TMessage);

    /// <summary>
    /// Sets the request value to publish without parsing the HTTP request body.
    /// </summary>
    /// <param name="value">The request value.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest, TMessage> WithValue(TRequest? value)
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
    public WebApiPublishOptions<TRequest, TMessage> WithValidator(IValidator<TRequest> validator)
    {
        Validator = validator.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Sets the event factory for the mapped value.
    /// </summary>
    /// <param name="messageFactory">The function that creates the event envelope.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest, TMessage> WithEventFactory(Func<TMessage, EventData> messageFactory)
    {
        _messageFactory = messageFactory.ThrowIfNull();
        return this;
    }

    /// <inheritdoc/>
    internal override Task<Result<WebApiPreparedPublish>> PrepareAsync<TResult>(WebApi<TResult> webApi, WebApiOptions requestOptions, CancellationToken cancellationToken)
        => _hasValue
            ? webApi.PreparePublishValueAsync(requestOptions, _value, Validator, CreateMessage, _operationKeyFactory, cancellationToken)
            : webApi.PreparePublishRequestAsync(requestOptions, Validator, CreateMessage, _operationKeyFactory, cancellationToken);

    /// <summary>
    /// Derives a repeatable submission key from the current Web API request and validated input before mapping it to the message.
    /// </summary>
    /// <param name="keyFactory">The request-aware submission-key selector.</param>
    /// <returns>The current options.</returns>
    public WebApiPublishOptions<TRequest, TMessage> WithOperationKey(Func<WebApiOptions, TRequest, CompositeKey> keyFactory)
    {
        _operationKeyFactory = keyFactory.ThrowIfNull();
        return this;
    }

    /// <summary>
    /// Maps the request and creates an event envelope using the configured factory or default settings.
    /// </summary>
    /// <param name="value">The request value.</param>
    /// <returns>The event envelope.</returns>
    private EventData CreateMessage(TRequest value)
    {
        var message = Mapper.Map(value);
        return _messageFactory?.Invoke(message) ?? CreateDefaultMessage(message);
    }
}
