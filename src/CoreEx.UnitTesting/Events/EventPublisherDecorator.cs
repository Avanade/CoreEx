namespace CoreEx.UnitTesting.Events;

/// <summary>
/// Provides a decorator for an event publisher that integrates with <see cref="TestSharedState"/> enabling additional test-related behaviors while delegating event publishing operations to the actual underlying publisher.
/// </summary>
/// <param name="key">The key used to reference the published events in the shared state.</param>
/// <param name="testSharedState">The shared test state used to coordinate or track event publishing during tests.</param>
/// <param name="innerEventPublisher">The underlying event publisher to which all event publishing operations are delegated.</param>
/// <param name="logger">The optional logger.</param>
/// <remarks>This decorator is typically used in testing scenarios to augment and observe event publishing without modifying the core event publisher implementation. All publishing operations are forwarded
/// to the specified inner event publisher.
/// <para>Where a <paramref name="logger"/> is provided, the full event payload for each event about to be published is logged at <see cref="LogLevel.Debug"/> prior to publishing; this is deliberately
/// <b>not</b> available on the production <see cref="CoreEx.Events.Publishing.EventPublisherBase"/> as it could result in sensitive payload data being logged in a deployed environment. As this decorator
/// only exists within the test pipeline, the same visibility here carries no such risk.</para></remarks>
public class EventPublisherDecorator(string key, TestSharedState testSharedState, IEventPublisher innerEventPublisher, ILogger<EventPublisherDecorator>? logger = null) : IEventPublisher
{
    private static readonly JsonSerializerOptions _debugJsonSerializerOptions = new() { WriteIndented = true };

    private readonly TestSharedState _sharedState = testSharedState.ThrowIfNull();
    private readonly IEventPublisher _innerEventPublisher = innerEventPublisher.ThrowIfNull();
    private readonly ILogger<EventPublisherDecorator>? _logger = logger;
    private readonly HashSet<DestinationEvent> _capturedEvents = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Gets the key used to reference the published events in the shared state.
    /// </summary>
    /// <remarks>This key is typically the same as used to register the underlying service itself.</remarks>
    public string Key { get; } = key.ThrowIfNullOrEmpty();

    /// <inheritdoc/>
    public bool HasBeenPublished => _innerEventPublisher.HasBeenPublished;

    /// <inheritdoc/>
    public bool IsEmpty => _innerEventPublisher.IsEmpty;

    /// <inheritdoc/>
    public int Count => _innerEventPublisher.Count;

    /// <inheritdoc/>
    public void Add(IEnumerable<EventData> events) => _innerEventPublisher.Add(events);

    /// <inheritdoc/>
    public void Add(string destination, IEnumerable<EventData> events) => _innerEventPublisher.Add(destination, events);

    /// <inheritdoc/>
    public void Add(string destination, IEnumerable<CloudEvent> events) => _innerEventPublisher.Add(destination, events);

    /// <inheritdoc/>
    public void Add(params EventData[] events) => _innerEventPublisher.Add(events);

    /// <inheritdoc/>
    public void Add(string destination, params EventData[] events) => _innerEventPublisher.Add(destination, events);

    /// <inheritdoc/>
    public void Add(string destination, params CloudEvent[] events) => _innerEventPublisher.Add(destination, events);

    /// <inheritdoc/>
    public void Add(IEnumerable<DestinationEvent> events) => _innerEventPublisher.Add(events);

    /// <inheritdoc/>
    public void Clear() => _innerEventPublisher.Clear();

    /// <inheritdoc/>
    public void Reset() => _innerEventPublisher.Reset();

    /// <inheritdoc/>
    public void Dequeue(int count) => _innerEventPublisher.Dequeue(count);

    /// <inheritdoc/>
    /// <remarks>Removes this decorator's own captured "published" events from <see cref="TestSharedState"/> for the current request - called where a surrounding unit-of-work transaction enlisted these
    /// events atomically alongside a business mutation (e.g. <c>CosmosDbUnitOfWork</c>), then subsequently failed to actually commit; from the test's perspective, nothing was really published.</remarks>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var requestId = _sharedState.GetHttpRequestId();
        lock (_capturedEvents)
        {
            if (_capturedEvents.Count > 0)
            {
                _sharedState.RequestStateData(requestId).AddOrUpdate(Key, Array.Empty<DestinationEvent>(), (_, existing) => GetCapturedEvents(existing).Where(e => !_capturedEvents.Contains(e)).ToArray());
                _capturedEvents.Clear();
            }
        }

        await _innerEventPublisher.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public DestinationEvent[] GetEvents() => _innerEventPublisher.GetEvents();

    /// <inheritdoc/>
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        var events = GetEvents();
        var requestId = _sharedState.GetHttpRequestId();

        // Where an action is registered in the shared state for the current request, invoke it; this allows for test-specific behaviors to be executed just prior to the actual publishing of events.
        if (_sharedState.RequestStateData(requestId).TryGetValue($"_{nameof(EventPublisherDecorator)}_{key}", out var val) && val is Action publishAction)
            publishAction();

        // Log contents of the events to be published at debug level, if a logger is provided and debug logging is enabled.
        if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
        {
            var list = events.Select(de => new { destination = de.Destination, @event = de.Event.EncodeToJsonElement() });
            _logger.LogDebug("Preparing to send {Length} event(s):{NewLine}{Json}", events.Length, Environment.NewLine, JsonSerializer.Serialize(list, _debugJsonSerializerOptions));
        }

        // Publish the events using the underlying publisher.
        await _innerEventPublisher.PublishAsync(cancellationToken).ConfigureAwait(false);

        // Give each capture its own identity so rollback removes only this decorator's publications.
        var captured = events.Select(e => new DestinationEvent(e.Destination, e.Event)).ToArray();
        lock (_capturedEvents)
        {
            _sharedState.RequestStateData(requestId).AddOrUpdate(Key, captured, (_, existing) => GetCapturedEvents(existing).Concat(captured).ToArray());
            _capturedEvents.UnionWith(captured);
        }
    }

    /// <summary>Reads the captured batch without silently replacing unexpected shared state.</summary>
    private static DestinationEvent[] GetCapturedEvents(object? value) => value as DestinationEvent[] ?? throw new InvalidOperationException("The captured publisher state must contain destination events.");
}
