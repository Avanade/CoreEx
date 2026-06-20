namespace CoreEx.Events;

/// <summary>
/// Provides the core event (message) data in a format agnostic manner.
/// </summary>
/// <remarks>Although the <see cref="EventData"/> is <see langword="sealed"/> extensions are supported; these should be implemented as extension properties/methods leveraging the underlying <see cref="Attributes"/> where applicable.</remarks>
public sealed partial class EventData : IIdentifier<string>, ITenantId, IPartitionKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventData"/> class.
    /// </summary>
    public EventData()
    {
        Id = Runtime.NewId();
        Timestamp = Runtime.UtcNow;

        if (ExecutionContext.TryGetCurrent(out var ec))
        {
            TenantId = ec.TenantId;
            UserType = ec.User.Type;
            UserId = ec.User.Id;
        }
    }

    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    /// <remarks>Defaults to <see cref="Runtime.NewId"/>.</remarks>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    /// <remarks>Defaults to <see cref="Runtime.UtcNow"/>.</remarks>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the domain (<see href="https://en.wikipedia.org/wiki/Domain-driven_design">DDD</see> bounded context) name.
    /// </summary>
    public string? DomainName { get; set; }

    /// <inheritdoc/>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the entity/command (subject) name.
    /// </summary>
    /// <remarks>This is typically a noun describing the <i>what</i> that is being acted upon such as '<c>Order</c>' or '<c>Customer</c>'.</remarks>
    public string? Entity { get; set; }

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    /// <remarks>This is typically a verb describing an <i>event</i> in past tense such as '<c>Created</c>' or '<c>Updated</c>'; or, describing a <i>command</i> such as '<c>Send</c>'.</remarks>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the unique key (or identifier) for the entity.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the trace parent (set to <see cref="System.Diagnostics.Activity.Id"/> via the <see cref="EventFormatter"/>).
    /// </summary>
    public string? TraceParent { get; set; }

    /// <summary>
    /// Gets or sets the trace state (set to <see cref="System.Diagnostics.Activity.TraceStateString"/> via the <see cref="EventFormatter"/>).
    /// </summary>
    public string? TraceState { get; set; }

    /// <summary>
    /// Gets or sets the trace baggage (set to <see cref="System.Diagnostics.Activity.Baggage"/> via the <see cref="EventFormatter"/>).
    /// </summary>
    public IEnumerable<KeyValuePair<string, string?>>? TraceBaggage { get; set; }

    /// <summary>
    /// Gets or sets the user <see cref="AuthenticationType"/> (defaults to <see cref="ExecutionContext.User"/> <see cref="AuthenticationUser.Type"/>).
    /// </summary>
    public AuthenticationType? UserType { get; set; }

    /// <summary>
    /// Gets or sets the user (auth) identifier (defaults to <see cref="ExecutionContext.User"/> <see cref="AuthenticationUser.Id"/>).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    /// <remarks>This is set automatically by the <see cref="WithValue{T}(T, IEnumerable{string})"/>.</remarks>
    public BinaryData? Data { get; set; }

    /// <summary>
    /// Gets or sets the event data schema <see cref="Uri"/>.
    /// </summary>
    /// <remarks>This is set automatically by the <see cref="WithValue{T}(T, IEnumerable{string})"/>.</remarks>
    public Uri? DataSchema { get; set; }

    /// <summary>
    /// Gets or sets the event data schema <see cref="Version"/>.
    /// </summary>
    /// <remarks>This is set automatically by the <see cref="WithValue{T}(T, IEnumerable{string})"/>.</remarks>
    public Version? DataSchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    /// <remarks>This is set automatically by the <see cref="WithValue{T}(T, IEnumerable{string})"/>; either with the implemented <see cref="IReadOnlyPartitionKey.PartitionKey"/> or falls back to the <see cref="Key"/> (see default <see cref="EventFormatter.Format(EventData)"/>).</remarks>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the reply-to destination.
    /// </summary>
    /// <remarks>This can be set to share a destination (i.e. topic) where a result is expected to be sent upon processing the event. For example, this can be used for commands to indicate where the result should be sent back to the caller.
    /// <para>Note: sending does not guarantee usage; it is up to the consumer as to whether to respond accordingly.</para></remarks>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the title; being the fully qualified (segmented) value used for routing, observability, policy enforcement, etc.
    /// </summary>
    /// <remarks>This is typically set/formatted when the event is being published.</remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the source <see cref="Uri"/>; typically the originating system or service that produced the event.
    /// </summary>
    /// <remarks>This is typically set/formatted when the event is being published.</remarks>
    public Uri? Source { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Events.MessageType"/>.
    /// </summary>
    /// <remarks>This is set by either the <see cref="CreateEvent(string, string?)"/> or <see cref="CreateCommandWith{T}(string, T, string)"/> to distinguish which was used; otherwise, defaults to <see cref="MessageType.Event"/>.
    /// <para>This property has no <see cref="CloudEvent"/> equivalence and is not converted by default.</para></remarks>
    [JsonIgnore]
    public MessageType MessageType { get; set; }

    /// <summary>
    /// Gets the additional attributes.
    /// </summary>
    /// <remarks>Any attribute key with an underscore ('<c>_</c>') prefix denotes that it is <i>not</i> intended to be published unless explicitly implemented.</remarks>
    [JsonIgnore]
    public ConcurrentDictionary<string, object?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds or updates the attribute using the specified <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The <see cref="EventData"/> to support fluent-style method chaining.</returns>
    public EventData SetAttribute<T>(string key, T value)
    {
        Attributes.AddOrUpdate(key.ThrowIfNullOrEmpty(), _ => value, (_, __) => value);
        return this;
    }

    /// <summary>
    /// Tries to get the attribute using the specified <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> indicates found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetAttribute<T>(string key, out T? value)
    {
        if (Attributes.TryGetValue(key, out var av))
        {
            value = (T?)av;
            return true;
        }

        value = default;
        return false;
    }
}