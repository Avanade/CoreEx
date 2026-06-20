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
    public string? MessageUniqueKey { get; set; }

    /// <summary>
    /// Gets or sets the resiliency attempt count (where applicable).
    /// </summary>
    /// <remarks>A value of zero indicates first execution; i.e. no retry performed.</remarks>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Gets or sets the corresponding subscribed <see cref="CloudEvent"/> (where applicable).
    /// </summary>
    public CloudEvent? CloudEvent { get; set; }

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
}