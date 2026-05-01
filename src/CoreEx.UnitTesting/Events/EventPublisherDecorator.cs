namespace CoreEx.UnitTesting.Events;

/// <summary>
/// Provides a decorator for an event publisher that integrates with <see cref="TestSharedState"/> enabling additional test-related behaviors while delegating event publishing operations to the actual underlying publisher.
/// </summary>
/// <param name="key">The key used to reference the published events in the shared state.</param>
/// <param name="testSharedState">The shared test state used to coordinate or track event publishing during tests.</param>
/// <param name="innerEventPublisher">The underlying event publisher to which all event publishing operations are delegated.</param>
/// <remarks>This decorator is typically used in testing scenarios to augment and observe event publishing without modifying the core event publisher implementation. All publishing operations are forwarded
/// to the specified inner event publisher.</remarks>
public class EventPublisherDecorator(string key, TestSharedState testSharedState, IEventPublisher innerEventPublisher) : IEventPublisher
{
    private readonly TestSharedState _sharedState = testSharedState.ThrowIfNull();
    private readonly IEventPublisher _innerEventPublisher = innerEventPublisher.ThrowIfNull();

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
    public void Rollback(int count) => _innerEventPublisher.Rollback(count);

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

        // Publish the events using the underlying publisher.
        await _innerEventPublisher.PublishAsync(cancellationToken).ConfigureAwait(false);

        // Forward the published events appending to the shared state.
        _sharedState.RequestStateData(requestId).AddOrUpdate(Key, events, (_, __) => events);
    }
}