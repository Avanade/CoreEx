namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the base event subscribing capabilities.
/// </summary>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The <see cref="ILogger{EventSubscriberBase}"/>.</param>
public abstract class EventSubscriberBase(IEventFormatter formatter, ILogger<EventSubscriberBase> logger)
{
    private static readonly ConcurrentDictionary<Type, SubscribeAttribute[]> _subscriptions = new();

    /// <summary>
    /// Gets the <see cref="IEventFormatter"/>.
    /// </summary>
    public IEventFormatter Formatter { get; } = formatter.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets or sets whether incoming baggage is imported into the current activity.
    /// </summary>
    /// <remarks>Defaults to false. Applies to CloudEvent envelopes and Service Bus transport properties; enable only for trusted producers. Existing keys are preserved and imports are bounded.</remarks>
    public bool PropagateBaggage { get; set; }

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
    /// <para>This is used by <see cref="DeserializeValue{TValue}(EventData, bool, ErrorHandling?, JsonSerializerOptions?)"/>.</para></remarks>
    public ErrorHandling InvalidDataHandling { get; set; } = ErrorHandling.CompleteAsError;

    /// <summary>
    /// Gets or sets the handling for a decoded CloudEvent that does not match any <see cref="SubscribeAttribute"/> on the concrete subscriber.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.DeadLetter"/>. With no attributes, no match is required. Malformed envelopes are handled separately by the transport.</remarks>
    public ErrorHandling UnmatchedCloudEventHandling { get; set; } = ErrorHandling.DeadLetter;

    /// <summary>Indicates whether the concrete subscriber declares any CloudEvent subscription rules.</summary>
    protected bool HasSubscriptions => Subscriptions.Length != 0;

    /// <summary>Gets the cached match rules declared on the concrete subscriber.</summary>
    private SubscribeAttribute[] Subscriptions => _subscriptions.GetOrAdd(GetType(), static type => [.. type.GetCustomAttributes<SubscribeAttribute>(inherit: false)]);

    /// <summary>
    /// Gets or sets the optional <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>This is used by <see cref="DeserializeValue{TValue}(EventData, bool, ErrorHandling?, JsonSerializerOptions?)"/>.</remarks>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets whether deserialized payload values are automatically cleaned before required-value checks and validation.
    /// </summary>
    /// <remarks>Defaults to false because published events are generally canonical already, making receive cleanup typically redundant, unlike WebApi request cleanup.
    /// Uses <see cref="CleanArgs.Default"/> without collapsing root or nested objects. The original envelope and message metadata are not changed.</remarks>
    public bool AutoCleanValue { get; set; }

    /// <summary>
    /// Gets the ordered <see cref="ISubscriberBehavior"/> registrations that wrap subscriber processing for each delivery attempt.
    /// </summary>
    /// <remarks>Configure during construction; the collection is not thread-safe for modification while receiving.</remarks>
    public SubscriberBehaviorCollection Behaviors { get; } = new();

    /// <summary>
    /// Receives a <see cref="CloudEvent"/>.
    /// </summary>
    /// <param name="cloudEvent">The <see cref="CloudEvent"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>Checks any declared <see cref="SubscribeAttribute"/> rules before converting the <paramref name="cloudEvent"/> into <see cref="EventData"/> and invoking <see cref="ReceiveAsync(EventData, EventSubscriberArgs?, CancellationToken)"/>.
    /// <para>Additionally, this method will specifically emit both the related tracing tags and the <see cref="EventSubscriberMetrics.MessagesReceived"/> for observability.</para></remarks>
    protected async Task<Result> ReceiveAsync(CloudEvent cloudEvent, EventSubscriberArgs? args = null, CancellationToken cancellationToken = default)
    {
        args ??= new EventSubscriberArgs();
        if (this != args.Owner)
            args.Owner = this;

        return await InvokeReceiveAttemptAsync(args, async c =>
        {
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

            return await ReceiveWrapperAsync(args, () =>
            {
                if (!MatchesSubscription(cloudEvent))
                {
                    const string reason = "The CloudEvent type and source did not match any [Subscribe] rule on the subscriber.";
                    return Task.FromResult(ClassifyReceiveFailure(args, new InvalidDataException(reason), UnmatchedCloudEventHandling, reason));
                }

                return ReceiveAsync(Formatter.ConvertFromCloudEvent(cloudEvent), args, c);
            }, c).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Checks whether the decoded CloudEvent is accepted by the subscriber.</summary>
    private bool MatchesSubscription(CloudEvent cloudEvent)
    {
        var subscriptions = Subscriptions;
        foreach (var subscription in subscriptions)
        {
            if (subscription.IsMatch(cloudEvent.Type, cloudEvent.Source))
                return true;
        }

        return subscriptions.Length == 0;
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
        return await InvokeReceiveAttemptAsync(args, async c =>
        {
            using var receiveScope = Correlation.BeginReceive(@event.CorrelationId, Logger);
            return await ReceiveEventAsync(@event, args, c).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Processes the authoritative envelope within the correlation receive boundary.</summary>
    private async Task<Result> ReceiveEventAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
    {
        if (this != args.Owner)
            args.Owner = this;

        // Link the producer without replacing the host activity; baggage propagation is a separate opt-in policy.
        if (!string.IsNullOrEmpty(@event.TraceParent) && Activity.Current is not null)
        {
            if (ActivityContext.TryParse(@event.TraceParent, @event.TraceState, isRemote: true, out var ac))
                Activity.Current.LinkTraceContext(ac);
        }

        if (PropagateBaggage)
            EventBaggage.Import(Activity.Current, @event.TraceBaggage);

        return await ReceiveWrapperAsync(args, async () =>
        {
            // Where the tenant is specified then confirm is same.
            if (TenantIdMismatchHandling.HasValue && !string.IsNullOrEmpty(@event.TenantId) && ExecutionContext.TryGetCurrent(out var ec) && ec.TenantId != @event.TenantId)
                return new EventSubscriberReceiveException($"{nameof(ExecutionContext.TenantId)} mismatch: {nameof(EventData)}.{nameof(EventData.TenantId)} '{@event.TenantId}' does not equal {nameof(ExecutionContext)}.{nameof(ExecutionContext.TenantId)} '{ec.TenantId}'.", TenantIdMismatchHandling.Value);

            return await InvokeBehaviorsAsync(args, @event, false, c => OnReceiveAsync(@event, args, c), cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
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
    /// <param name="args">The subscriber arguments.</param>
    /// <param name="receiveAsync">The processing callback.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The classified processing result.</returns>
    protected async Task<Result> ReceiveWrapperAsync(EventSubscriberArgs args, Func<Task<Result>> receiveAsync, CancellationToken cancellationToken)
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
            return ClassifyReceiveFailure(args, rex, rex.ErrorHandling);
        else if (result.Error is not IEventSubscriberException && !result.Error.IsCanceledBy(cancellationToken)) // Ignore IEventSubscriberException's; and only ignore a cancellation that is attributable to *this* receive's own cancellationToken (e.g. host shutdown) as that is intended to bubble up - any other (unrelated) cancellation is classified as normal.
            return ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), Exception = result.Error }, defaultErrorHandling: UnhandledErrorHandling);

        return result;
    }

    /// <summary>Classifies a known receive failure using its explicit disposition and an optional safe broker-facing reason.</summary>
    private Result ClassifyReceiveFailure(EventSubscriberArgs args, Exception exception, ErrorHandling errorHandling, string? deadLetterReason = null)
        => ErrorHandler.Handle(new ErrorHandlerArgs
        {
            SubscriberArgs = args,
            SourceType = GetType(),
            Exception = exception,
            ErrorHandlingOverride = errorHandling,
            DeadLetterReason = deadLetterReason
        }, defaultErrorHandling: UnhandledErrorHandling);

    /// <summary>
    /// Invokes a receive attempt, sharing the active attempt with nested receives using the same arguments.
    /// </summary>
    /// <param name="args">The per-delivery subscriber arguments.</param>
    /// <param name="receiveAsync">The receive function.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The receive result.</returns>
    /// <remarks>Transports should wrap their outermost receive processing so preprocessing is included. Use fresh arguments for each delivery and reuse them for its in-process retries.</remarks>
    protected async Task<Result> InvokeReceiveAttemptAsync(EventSubscriberArgs args, Func<CancellationToken, Task<Result>> receiveAsync, CancellationToken cancellationToken)
    {
        args.ThrowIfNull();
        receiveAsync.ThrowIfNull();
        if (args.AttemptActive)
            return await receiveAsync(cancellationToken).ConfigureAwait(false);

        args.ResultingException = null;
        args.ResultingErrorHandling = null;
        if (args.AttemptEntered)
            args.AttemptCount++;
        else
            args.AttemptEntered = true;

        args.AttemptActive = true;
        try
        {
            return await receiveAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            args.AttemptActive = false;
        }
    }

    /// <summary>
    /// Invokes the <paramref name="receiveAsync"/> function for a single delivery attempt, wrapped by the configured <see cref="Behaviors"/>.
    /// </summary>
    /// <param name="args">The per-delivery subscriber arguments.</param>
    /// <param name="event">The decoded <see cref="EventData"/>; <see langword="null"/> for native messages.</param>
    /// <param name="isNative">Indicates whether a native (non-CloudEvent) message is being processed.</param>
    /// <param name="receiveAsync">The subscriber processing function.</param>
    /// <param name="cancellationToken">The receive cancellation token.</param>
    /// <returns>The classified processing result.</returns>
    /// <remarks>Where no behaviors are configured, or the behavior pipeline is already active for the <paramref name="args"/>, the <paramref name="receiveAsync"/> is invoked directly. Otherwise, processing failures are
    /// classified using the <see cref="ErrorHandler"/> before being returned to the behaviors; an exception thrown (or an unclassified failure returned) by a behavior is treated as <see cref="ErrorHandling.None"/>.
    /// <para>The receiving host is then responsible for invoking <see cref="NotifyDispositionAsync"/> once the delivery has been settled.</para></remarks>
    protected Task<Result> InvokeBehaviorsAsync(EventSubscriberArgs args, EventData? @event, bool isNative, Func<CancellationToken, Task<Result>> receiveAsync, CancellationToken cancellationToken)
    {
        args.ThrowIfNull();
        receiveAsync.ThrowIfNull();
        return InvokeReceiveAttemptAsync(args, async c =>
        {
            if (Behaviors.Count == 0 || args.BehaviorActive)
                return await receiveAsync(c).ConfigureAwait(false);

            if (this != args.Owner)
                args.Owner = this;

            args.Event = @event;
            args.IsNative = isNative;
            args.BehaviorActive = true;
            try
            {
                ISubscriberBehavior[] behaviors;
                try
                {
                    behaviors = args.Behaviors ??= Behaviors.Resolve();
                }
                catch (Exception ex)
                {
                    return HandleBehaviorFailure(args, typeof(SubscriberBehaviorCollection), ex, c);
                }

                return await InvokeBehaviorAsync(args, behaviors, 0, receiveAsync, c).ConfigureAwait(false);
            }
            finally
            {
                args.BehaviorActive = false;
            }
        }, cancellationToken);
    }

    /// <summary>Invokes the behavior at the specified <paramref name="index"/>, or the subscriber where all behaviors have been invoked.</summary>
    private async Task<Result> InvokeBehaviorAsync(EventSubscriberArgs args, ISubscriberBehavior[] behaviors, int index, Func<CancellationToken, Task<Result>> receiveAsync, CancellationToken cancellationToken)
    {
        if (index >= behaviors.Length)
            return await ReceiveWrapperAsync(args, () => receiveAsync(cancellationToken), cancellationToken).ConfigureAwait(false);

        var behavior = behaviors[index];
        if (args.ParticipatedCount <= index)
            args.ParticipatedCount = index + 1;

        var nextInvoked = false;
        Result result;
        try
        {
            result = await behavior.InvokeAsync(args, ct =>
            {
                if (nextInvoked)
                    throw new InvalidOperationException($"Subscriber behavior '{behavior.GetType().Name}' invoked next more than once within a single attempt.");

                nextInvoked = true;
                return InvokeBehaviorAsync(args, behaviors, index + 1, receiveAsync, ct);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return HandleBehaviorFailure(args, behavior.GetType(), ex, cancellationToken);
        }

        if (result.IsSuccess || result.Error is IEventSubscriberException || result.Error.IsCanceledBy(cancellationToken))
            return result;

        if (result.Error is EventSubscriberReceiveException rex)
            return ClassifyBehaviorResult(args, behavior.GetType(), rex);

        return HandleBehaviorFailure(args, behavior.GetType(), result.Error, cancellationToken);
    }

    /// <summary>Classifies a <see cref="SubscriberBehaviorResult"/> short-circuit using its explicit handling.</summary>
    private Result ClassifyBehaviorResult(EventSubscriberArgs args, Type behaviorType, EventSubscriberReceiveException exception)
        => ErrorHandler.Handle(new ErrorHandlerArgs
        {
            SubscriberArgs = args,
            SourceType = behaviorType,
            Exception = exception,
            ErrorHandlingOverride = exception.ErrorHandling,
            DeadLetterReason = exception.ErrorHandling == ErrorHandling.DeadLetter ? exception.Message : null
        }, defaultErrorHandling: UnhandledErrorHandling);

    /// <summary>Classifies a behavior failure as <see cref="ErrorHandling.None"/>; already-classified failures and receive cancellation pass through.</summary>
    private Result HandleBehaviorFailure(EventSubscriberArgs args, Type behaviorType, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is IEventSubscriberException || exception.IsCanceledBy(cancellationToken))
            return Result.Fail(exception);

        if (exception is EventSubscriberReceiveException rex)
            return ClassifyBehaviorResult(args, behaviorType, rex);

        return ErrorHandler.Handle(new ErrorHandlerArgs
        {
            SubscriberArgs = args,
            SourceType = behaviorType,
            Exception = exception,
            ErrorHandlingOverride = ErrorHandling.None
        }, defaultErrorHandling: ErrorHandling.None);
    }

    /// <summary>
    /// Notifies the participating <see cref="Behaviors"/> of the settlement actually applied to the delivery.
    /// </summary>
    /// <param name="args">The per-delivery subscriber arguments used for the receive.</param>
    /// <param name="disposition">The <see cref="SubscriberDisposition"/>.</param>
    /// <param name="cancellationToken">The receive cancellation token; this may already be cancelled.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    /// <remarks>Intended to be invoked by the receiving host exactly once per delivery, after settlement. No operation where no behavior participated, or where already notified. Callbacks are invoked in
    /// reverse registration order; each callback failure is logged as a warning and suppressed so that it can never change the settlement.</remarks>
    public async Task NotifyDispositionAsync(EventSubscriberArgs args, SubscriberDisposition disposition, CancellationToken cancellationToken = default)
    {
        args.ThrowIfNull();
        disposition.ThrowIfNull();
        if (args.Behaviors is null || args.ParticipatedCount == 0)
            return;

        if (args.DispositionNotified)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("Subscriber '{Subscriber}' disposition has already been notified for the delivery; ignored.", GetType().Name);

            return;
        }

        args.DispositionNotified = true;
        for (var i = Math.Min(args.ParticipatedCount, args.Behaviors.Length) - 1; i >= 0; i--)
        {
            var behavior = args.Behaviors[i];
            try
            {
                await behavior.OnDispositionAsync(args, disposition, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Subscriber '{Subscriber}' behavior '{Behavior}' disposition callback failed for settlement '{Settlement}'; the failure has been ignored: {Error}", GetType().Name, behavior.GetType().Name, disposition.Settlement, ex.Message);
            }
        }
    }

    /// <summary>
    /// Creates an invalid-data result for a transport-specific payload failure.
    /// </summary>
    /// <typeparam name="TValue">The payload type.</typeparam>
    /// <param name="message">The failure message.</param>
    /// <param name="exception">The optional underlying exception.</param>
    /// <returns>A failure using the configured invalid-data disposition.</returns>
    protected Result<TValue> InvalidDataResult<TValue>(string message, Exception? exception = null)
        => new EventSubscriberReceiveException(message, InvalidDataHandling, exception);

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
        => DeserializeValue<TValue>(@event, valueIsRequired, invalidDataHandling, jsonSerializerOptions, null);

    /// <summary>
    /// Deserializes a payload with an optional override of <see cref="AutoCleanValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The payload type.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="valueIsRequired">Whether the cleaned value is required.</param>
    /// <param name="invalidDataHandling">The optional invalid-data policy override.</param>
    /// <param name="jsonSerializerOptions">The optional serializer options override.</param>
    /// <param name="autoCleanValue">The cleanup override; null uses <see cref="AutoCleanValue"/>.</param>
    /// <returns>The deserialized and optionally cleaned value or an invalid-data failure.</returns>
    /// <remarks>Cleanup failures propagate to normal receive error handling, not the invalid-data policy.</remarks>
    public Result<TValue> DeserializeValue<TValue>(EventData @event, bool valueIsRequired, ErrorHandling? invalidDataHandling, JsonSerializerOptions? jsonSerializerOptions, bool? autoCleanValue)
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

        if (autoCleanValue ?? AutoCleanValue)
            value = Metadata.RuntimeMetadata.Clean(value, CleanArgs.Default);

        if (!valueIsRequired)
            return Result.Ok(value!); // Optional payloads intentionally permit null; use a nullable TValue.

        return Result.Go(value!).Required() // Required reports a null cleaned payload through the existing invalid-data policy.
            .OnFailure(r => new EventSubscriberReceiveException("An error occurred in the event subscriber as the deserialized value is required.", invalidDataHandling ?? InvalidDataHandling, r.Error));
    }
}
