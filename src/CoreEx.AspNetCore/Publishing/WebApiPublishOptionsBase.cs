namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Provides shared configuration and message preparation for Web API publishing.
/// </summary>
public abstract class WebApiPublishOptionsBase
{
    private Func<EventData, EventData>? _eventTransform;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiPublishOptionsBase"/> class.
    /// </summary>
    private protected WebApiPublishOptionsBase() { }

    /// <summary>
    /// Gets the optional destination override.
    /// </summary>
    public string? Destination { get; private protected set; }

    /// <summary>
    /// Gets the optional publisher resolver.
    /// </summary>
    public Func<IServiceProvider, IEventPublisher>? PublisherProvider { get; private protected set; }

    /// <summary>
    /// Gets the optional operation timing configuration.
    /// </summary>
    public WebApiPublishOperationOptions? Operation { get; private protected set; }

    /// <summary>
    /// Gets the message payload type used to resolve tracked operation registration.
    /// </summary>
    internal abstract Type PublishedType { get; }

    /// <summary>
    /// Prepares the message before the Web API publishing and tracking pipeline dispatches it.
    /// </summary>
    /// <typeparam name="TResult">The Web API result type.</typeparam>
    /// <param name="webApi">The Web API instance.</param>
    /// <param name="requestOptions">The options for the current request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The prepared message or a failure.</returns>
    internal abstract Task<Result<EventData>> PrepareAsync<TResult>(WebApi<TResult> webApi, WebApiOptions requestOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the default self-addressed <see cref="CoreEx.Events.MessageType.Command"/> message, with the <see cref="EventAction.Publish"/> action, for the specified payload.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="value">The message payload.</param>
    /// <returns>The new event data.</returns>
    /// <remarks>Use <see cref="WebApiPublishOptionsBase{TSelf}.WithEvent(Func{EventData, EventData})"/> to customize the message type, domain, action or other envelope properties.</remarks>
    internal static EventData CreateDefaultMessage<T>(T value) => new EventData { MessageType = CoreEx.Events.MessageType.Command }.WithAction(EventAction.Publish).WithValue(value);

    /// <summary>
    /// Applies the configured event transformation.
    /// </summary>
    /// <param name="message">The prepared message.</param>
    /// <returns>The transformed message.</returns>
    internal EventData ApplyEventTransform(EventData message) => _eventTransform is null ? message : _eventTransform(message).ThrowIfNull();

    /// <summary>
    /// Sets the event transformation.
    /// </summary>
    /// <param name="eventTransform">The event transformation.</param>
    private protected void SetEventTransform(Func<EventData, EventData> eventTransform) => _eventTransform = eventTransform;
}
