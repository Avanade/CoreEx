namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the base event subscribing capabilities.
/// </summary>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The <see cref="ILogger{EventSubscriberBase}"/>.</param>
public abstract class EventSubscriberBase(IEventFormatter formatter, ILogger<EventSubscriberBase> logger)
{
    /// <summary>
    /// Gets the <see cref="IEventFormatter"/>.
    /// </summary>
    public IEventFormatter Formatter { get; } = formatter.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets or sets the customizable <see cref="Subscribing.ErrorHandler"/> configuration.
    /// </summary>
    /// <remarks>The <see cref="ErrorHandler.AutoTransientHandling"/> is defaulted to <see langword="true"/> and the <see cref="ErrorHandler.WhereIsExtendedErrorHandling"/> is defaulted to <see cref="ErrorHandling.CompleteAsError"/>.</remarks>
    public ErrorHandler ErrorHandler { get; set => field = value.ThrowIfNull(); } = new() { AutoTransientHandling = true, WhereIsExtendedErrorHandling = ErrorHandling.CompleteAsError };

    /// <summary>
    /// Gets or sets the unhandled <see cref="ErrorHandling"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.None"/>.
    /// <para>This is a catch all for any <see cref="Exception"/> not explicity configured within the <see cref="ErrorHandler"/>.</para></remarks>
    public ErrorHandling UnhandledErrorHandling { get; set; } = ErrorHandling.None;

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> for when the <see cref="EventData.TenantId"/> does not equal the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.Catastrophic"/>.
    /// <para>Set to <see langword="null"/> to bypass check.</para></remarks>
    public ErrorHandling? TenantIdMismatchHandling { get; set; } = ErrorHandling.Catastrophic;

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> where the <see cref="EventData.Data"/> is not considered <i>valid</i>.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.CompleteAsError"/> as it is considered poison and as such is unable to be processed.
    /// <para>This is used by <see cref="DeserializeValue{TValue}"/>.</para></remarks>
    public ErrorHandling InvalidDataHandling { get; set; } = ErrorHandling.CompleteAsError;

    /// <summary>
    /// Gets or sets the optional <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>This is used by <see cref="DeserializeValue{TValue}"/>.</remarks>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Receives a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>This will <see cref="IEventFormatter.ConvertFromCloudEvent(CloudEvent)"/> the <paramref name="cloudEvent"/> into an <see cref="EventData"/> and invoke the <see cref="ReceiveAsync(EventData, EventSubscriberArgs?, CancellationToken)"/>
    /// <para>Additionally, this method will specifically emit both the related tracing tags and the <see cref="EventSubscriberMetrics.MessagesReceived"/> for observability.</para></remarks>
    protected async Task<Result> ReceiveAsync(CloudEvent cloudEvent, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        args ??= new EventSubscriberArgs();
        if (this != args.Owner)
            args.Owner = this;

        args.CloudEvent = cloudEvent;

        // Add tracing (where applicable).
        if (Activity.Current is not null)
        {
            Activity.Current.SetTag("cloudevents.event_id", cloudEvent.Id);
            Activity.Current.SetTag("cloudevents.event_source", cloudEvent.Source);
            Activity.Current.SetTag("cloudevents.event_type", cloudEvent.Type);
            Activity.Current.SetTag("cloudevents.event_subject", cloudEvent.Subject);
            Activity.Current.SetTag("cloudevents.event_spec_version", cloudEvent.SpecVersion.VersionId);
        }

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Received CloudEvent with Id='{CloudEventId}', Source='{CloudEventSource}', Type='{CloudEventType}', Subject='{CloudEventSubject}'.", cloudEvent.Id, cloudEvent.Source, cloudEvent.Type, cloudEvent.Subject);

        // Convert to an EventData and process.
        return await ReceiveWrapperAsync(args, async () => await ReceiveAsync(Formatter.ConvertFromCloudEvent(cloudEvent), args, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <summary>
    /// Receives an <see cref="EventData"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>This invokes the <see cref="OnReceiveAsync(EventData, EventSubscriberArgs, CancellationToken)"/> to perform the specific receive work.
    /// <para>This will also link the underlying <see cref="Activity.Current"/> to the originating <see cref="EventData.TraceParent"/> and <see cref="EventData.TraceState"/> where applicable.</para></remarks>
    protected async Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        args ??= new EventSubscriberArgs();
        if (this != args.Owner)
            args.Owner = this;

        // When the event has tracing we should link and include any baggage. We link as we don't want to override the current activity but rather link to the originating event's activity.
        if (!string.IsNullOrEmpty(@event.TraceParent) && Activity.Current is not null)
        {
            if (ActivityContext.TryParse(@event.TraceParent, @event.TraceState, out var ac))
            {
                Activity.Current?.AddLink(new ActivityLink(ac));
                if (@event.TraceBaggage is not null)
                {
                    foreach (var kvp in @event.TraceBaggage)
                        Activity.Current?.AddBaggage(kvp.Key, kvp.Value);
                }
            }
        }

        return await ReceiveWrapperAsync(args, async () =>
        {
            // Where the tenant is specified then confirm is same.
            if (TenantIdMismatchHandling.HasValue && !string.IsNullOrEmpty(@event.TenantId) && ExecutionContext.TryGetCurrent(out var ec) && ec.TenantId != @event.TenantId)
                return new EventSubscriberReceiveException($"{nameof(ExecutionContext.TenantId)} mismatch: {nameof(EventData)}.{nameof(EventData.TenantId)} '{@event.TenantId}' does not equal {nameof(ExecutionContext)}.{nameof(ExecutionContext.TenantId)} '{ec.TenantId}'.", TenantIdMismatchHandling.Value);

            return await OnReceiveAsync(@event, args ??= new EventSubscriberArgs(), cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Receives and processes the <see cref="EventData"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    protected abstract Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken);

    /// <summary>
    /// Receives and processes the <paramref name="receiveAsync"/> function with standardized execution and error/exception handling.
    /// </summary>
    private async Task<Result> ReceiveWrapperAsync(EventSubscriberArgs args, Func<Task<Result>> receiveAsync)
    {
        // Execute the internal receive logic.
        Result result;
        try
        {
            result = await receiveAsync().ConfigureAwait(false);
            if (result.IsSuccess)
                return result;
        }
        catch (Exception ex) // Capture any unhandled exception.
        {
            result = Result.Fail(ex);
        }

        // Apply standardized error/exception handling where applicable.
        if (result.Error is EventSubscriberReceiveException rex) // Expected with self declared error handling.
            return ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), ErrorHandlingOverride = rex.ErrorHandling, Exception = rex }, defaultErrorHandling: UnhandledErrorHandling);
        else if (result.Error is not IEventSubscriberException && !result.Error.IsCanceled()) // Ignore IEventSubscriberException's and *CanceledException as they are intended to bubble up!
            return ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), Exception = result.Error }, defaultErrorHandling: UnhandledErrorHandling);

        return result;
    }

    /// <summary>
    /// Deserializes the <see cref="EventData.Data"/> value to the specified <typeparamref name="TValue"/> type.
    /// </summary>
    /// <typeparam name="TValue">The <see cref="EventData.Data"/> value <see cref="Type"/>.</typeparam>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="valueIsRequired">Indicates whether the <see cref="EventData.Data"/> value is required.</param>
    /// <param name="invalidDataHandling">The optional <see cref="InvalidDataHandling"/> override.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/> override.</param>
    /// <returns>The deserialized value as a <see cref="Result{T}.Value"/>.</returns>
    /// <remarks>Where an invalid data, deserialization or required, error occurs an <see cref="EventSubscriberReceiveException"/> will be be returned via a <see cref="Result.Fail(Exception)"/>.</remarks>
    public Result<TValue> DeserializeValue<TValue>(EventData @event, bool valueIsRequired = true, ErrorHandling? invalidDataHandling = null, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        @event.ThrowIfNull();

        TValue? value;
        try
        {
            value = @event.ToObjectFromJson<TValue>(jsonSerializerOptions ?? JsonSerializerOptions ?? JsonDefaults.SerializerOptions)!;
        }
        catch (Exception ex)
        {
            return new EventSubscriberReceiveException("An error occurred in the event subscriber during event data deserialization.", invalidDataHandling ?? InvalidDataHandling, ex);
        }

        if (!valueIsRequired)
            return value;

        return Result.Go(value).Required()
            .OnFailure(r => new EventSubscriberReceiveException("An error occurred in the event subscriber as the deserialized value is required.", invalidDataHandling ?? InvalidDataHandling, r.Error));
    }
}