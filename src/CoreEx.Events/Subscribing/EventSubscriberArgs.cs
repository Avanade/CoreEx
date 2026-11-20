namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides <see cref="EventSubscriberBase"/> arguments.
/// </summary>
/// <remarks>Where additional properties are required then leverage the <see cref="Properties"/> dictionary.</remarks>
public sealed class EventSubscriberArgs
{
    /// <summary>
    /// Gets or sets the owning <see cref="EventSubscriberBase"/>.
    /// </summary>
    public EventSubscriberBase? Owner { get; set => field = field is null ? value : throw new InvalidOperationException($"The {nameof(Owner)} is immutable."); }

    /// <summary>
    /// Gets the properties <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets the messaging system unique key.
    /// </summary>
    /// <remarks>This is intended to provide a unique key related to the actual underlying messaging system receive/consume. For example, a log-based messaging system, such as Kafka, this may 
    /// be the composition of the topic, partition and offset. And for a AMPQ-based messaging system, such as Azure Service Bus, this may be the composition of the topic (or subscription) and sequence number.
    /// <para>This provides <i>uniqueness</i> beyond the likes of the <see cref="EventData.Id"/> which has no messaging system uniqueness guarantees.</para></remarks>
    public string? MessageUniqueKey { get; set => field = field is null ? value : throw new InvalidOperationException($"The {nameof(MessageUniqueKey)} is immutable."); }

    /// <summary>
    /// Gets the in-process attempt count for the delivery.
    /// </summary>
    /// <remarks>A value of zero indicates the first execution; it is incremented before each in-process retry using the same arguments, regardless of whether behaviors are registered.
    /// Nested receives using the same arguments do not count as retries. Use fresh arguments for each broker delivery; this is not the broker delivery count.</remarks>
    public int AttemptCount { get; internal set; }

    /// <summary>
    /// Gets the corresponding subscribed <see cref="CloudEvent"/> (where applicable).
    /// </summary>
    public CloudEvent? CloudEvent { get; internal set; }

    /// <summary>
    /// Gets the decoded <see cref="EventData"/> for the current attempt; <see langword="null"/> for native messages.
    /// </summary>
    public EventData? Event { get; internal set; }

    /// <summary>
    /// Indicates whether the current attempt is processing a native (non-CloudEvent) message.
    /// </summary>
    public bool IsNative { get; internal set; }

    /// <summary>
    /// Gets the resulting <see cref="Exception"/> where there was an error processing the event.
    /// </summary>
    public Exception? ResultingException { get; internal set; }

    /// <summary>
    /// Gets the resulting <see cref="ErrorHandling"/> where there was a <see cref="ResultingException"/>.
    /// </summary>
    public ErrorHandling? ResultingErrorHandling { get; internal set; }

    /// <summary>
    /// Indicates whether the <see cref="SubscribedManager"/> is being used.
    /// </summary>
    public bool UsesSubscribedManager { get; internal set; }

    /// <summary>
    /// Gets the matched <see cref="SubscribedBase"/> instance where the <see cref="SubscribedManager"/> is being used.
    /// </summary>
    /// <remarks>Where the <see cref="SubscribedManager"/> is not being used then this will always be <see langword="null"/>. Where the <see cref="UsesSubscribedManager"/> is <see langword="true"/>, this will
    /// be the matched <see cref="SubscribedBase"/> instance; and in this case where <see langword="null"/> this indicates that no matching subscriber was found.</remarks>
    public SubscribedBase? Subscriber { get; internal set; }

    /// <summary>
    /// Gets or sets the behavior instances resolved for the delivery.
    /// </summary>
    internal ISubscriberBehavior[]? Behaviors { get; set; }

    /// <summary>
    /// Gets or sets the number of behaviors (outermost-first) that participated in the delivery.
    /// </summary>
    internal int ParticipatedCount { get; set; }

    /// <summary>
    /// Indicates whether an attempt has been entered at least once for the delivery.
    /// </summary>
    internal bool AttemptEntered { get; set; }

    /// <summary>
    /// Indicates whether a receive attempt is currently active.
    /// </summary>
    internal bool AttemptActive { get; set; }

    /// <summary>
    /// Indicates whether the behavior pipeline is currently active.
    /// </summary>
    internal bool BehaviorActive { get; set; }

    /// <summary>
    /// Indicates whether the disposition has been notified.
    /// </summary>
    internal bool DispositionNotified { get; set; }
}
