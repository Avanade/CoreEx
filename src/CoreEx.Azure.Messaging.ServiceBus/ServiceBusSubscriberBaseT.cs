namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Receives a uniform payload from native JSON or CloudEvent messages with automatic deserialization, optional validation and standardized error handling.
/// </summary>
/// <typeparam name="TValue">The message body type.</typeparam>
/// <remarks>Defaults to CloudEvent processing. Native JSON requires <see cref="ServiceBusSubscriberBase.DataRepresentation"/> to be explicitly set to <see cref="MessageRepresentation.Native"/>. No subject dispatch occurs. Settlement remains the receiving host's responsibility.</remarks>
public abstract class ServiceBusSubscriberBase<TValue> : ServiceBusSubscriberBase
{
    /// <summary>
    /// Initializes a subscriber using the default event formatter without requiring formatter registration.
    /// </summary>
    /// <param name="logger">The subscriber logger.</param>
    protected ServiceBusSubscriberBase(ILogger<ServiceBusSubscriberBase> logger) : base(logger) { }

    /// <summary>
    /// Initializes a subscriber using the specified event formatter.
    /// </summary>
    /// <param name="formatter">The event formatter used for CloudEvent processing.</param>
    /// <param name="logger">The subscriber logger.</param>
    protected ServiceBusSubscriberBase(IEventFormatter formatter, ILogger<ServiceBusSubscriberBase> logger) : base(formatter, logger) { }

    /// <summary>
    /// Indicates whether the deserialized value is required.
    /// </summary>
    /// <remarks>Defaults to true. Override false for optional values, using a nullable <typeparamref name="TValue"/> for reference payloads.</remarks>
    public virtual bool ValueIsRequired { get; } = true;

    /// <summary>
    /// Gets the optional validator invoked before the typed receive handler.
    /// </summary>
    /// <remarks>Validation failures use <see cref="EventSubscriberBase.ErrorHandler"/>, separately from <see cref="EventSubscriberBase.InvalidDataHandling"/> for malformed or missing bodies.</remarks>
    public virtual IValidator<TValue>? ValueValidator { get; }

    /// <inheritdoc/>
    protected sealed override Task<Result> OnReceiveMessageAsync(ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => DataRepresentation == MessageRepresentation.CloudEvent
            ? base.OnReceiveMessageAsync(message, args, cancellationToken)
            : ReceiveWrapperAsync(args, async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await ReceiveValueAsync(DeserializeValue(message), message, args, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);

    /// <inheritdoc/>
    protected sealed override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        => ReceiveValueAsync(DeserializeValue<TValue>(@event, ValueIsRequired), args.Message, args, cancellationToken);

    /// <summary>
    /// Validates the deserialized payload and invokes the typed handler for either representation.
    /// </summary>
    private async Task<Result> ReceiveValueAsync(Result<TValue> result, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (result.IsFailure)
            return result.AsResult();

        var validator = ValueValidator;
        if (validator is not null)
        {
            var validation = await validator.ValidateAsync(result.Value, cancellationToken).ConfigureAwait(false);
            if (validation.HasErrors)
                return validation.ToResult();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await OnReceiveAsync(result.Value, message, args, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the deserialized and validated message body.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <param name="message">The original message, including its transport metadata.</param>
    /// <param name="args">The subscriber arguments.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The processing result.</returns>
    /// <remarks>In CloudEvent mode, <see cref="EventSubscriberArgs.CloudEvent"/> contains the decoded envelope. The original transport message is preserved in both modes.</remarks>
    protected abstract Task<Result> OnReceiveAsync(TValue value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the native JSON body using the configured serializer options and invalid-data policy.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>The deserialized value or an invalid-data failure.</returns>
    private Result<TValue> DeserializeValue(ServiceBusReceivedMessage message)
    {
        TValue? value;
        try
        {
            value = JsonSerializer.Deserialize<TValue>(message.Body.ToMemory().Span, JsonSerializerOptions ?? JsonDefaults.SerializerOptions);
        }
        catch (JsonException ex)
        {
            return InvalidDataResult<TValue>("An error occurred in the event subscriber during message body deserialization.", ex);
        }

        if (AutoCleanValue)
            value = Metadata.RuntimeMetadata.Clean(value, Entities.CleanArgs.Default);

        if (!ValueIsRequired)
            return Result.Ok(value!);   // Optional payloads intentionally allow null; use a nullable TValue when overriding ValueIsRequired.

        if (value is null)
            return InvalidDataResult<TValue>("An error occurred in the event subscriber as the deserialized value is required.");

        return Result.Go(value).Required()
            .OnFailure(r => InvalidDataResult<TValue>("An error occurred in the event subscriber as the deserialized value is required.", r.Error).Error);
    }
}
