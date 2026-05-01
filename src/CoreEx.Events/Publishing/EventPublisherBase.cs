namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides the base standardized <b>Event</b> publishing and sending orchestration.
/// </summary>
/// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The optional logger.</param>
/// <remarks>The <paramref name="destinationProvider"/> <see cref="IDestinationProvider.CreateFrom(CoreEx.Events.EventData, bool)"/> is used to dynamically generate the <i>default</i> destination when adding events using <see cref="Add(IEnumerable{EventData})"/>.</remarks>
public abstract class EventPublisherBase(IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<EventPublisherBase>? logger = null) : IEventPublisher
{
    private static JsonSerializerOptions? _debugJsonSerializerOptions;

    private readonly LinkedList<DestinationEvent> _queue = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private EventPublisherInvoker? _invoker;

    /// <summary>
    /// Gets the <see cref="IEventFormatter"/>.
    /// </summary>
    public IEventFormatter Formatter { get; } = formatter ?? new EventFormatter();

    /// <summary>
    /// Gets the <see cref="IDestinationProvider"/> that provides the destination (i.e. topic/queue) name for the events.
    /// </summary>
    /// <remarks>Defaults to <see cref="FixedDestinationProvider.Default"/>.</remarks>
    public IDestinationProvider DestinationProvider { get; } = destinationProvider ?? FixedDestinationProvider.Default;

    /// <summary>
    /// Gets the optional <see cref="ILogger"/> instance.
    /// </summary>
    public ILogger? Logger = logger;

    /// <summary>
    /// Gets the <see cref="EventPublisherInvoker"/>.
    /// </summary>
    protected EventPublisherInvoker Invoker => _invoker ??= EventPublisherInvoker.Default;

    /// <inheritdoc/>
    public bool IsEmpty => _queue.Count == 0;

    /// <inheritdoc/>
    public int Count => _queue.Count;

    /// <inheritdoc/>
    public bool HasBeenPublished { get; private set; }

    /// <inheritdoc/>
    public void Add(params IEnumerable<DestinationEvent> events)
    {
        Synchronize(() =>
        {
            foreach (var de in events)
            {
                if (de is not null)
                    _queue.AddLast(de);
            }
        });
    }

    /// <inheritdoc/>
    public void Add(params IEnumerable<EventData> events)
    {
        Synchronize(() =>
        {
            foreach (var @event in events)
            {
                if (@event is not null)
                    _queue.AddLast(new DestinationEvent(DestinationProvider.CreateFrom(@event), Formatter.ConvertToCloudEvent(Formatter.Format(@event))));
            }
        });
    }

    /// <inheritdoc/>
    public void Add(string destination, params IEnumerable<EventData> events)
    {
        destination.ThrowIfNullOrEmpty();
        Synchronize(() =>
        {
            foreach (var @event in events)
            {
                if (@event is not null)
                    _queue.AddLast(new DestinationEvent(destination, Formatter.ConvertToCloudEvent(Formatter.Format(@event))));
            }
        });
    }

    /// <inheritdoc/>
    public void Add(string destination, params IEnumerable<CloudEvent> events)
    {
        destination.ThrowIfNullOrEmpty();
        Synchronize(() =>
        {
            foreach (var @event in events)
            {
                if (@event is not null)
                    _queue.AddLast(new DestinationEvent(destination, @event));
            }
        });
    }

    /// <summary>
    /// Synchronizes the execution of the <paramref name="action"/> using a semaphore to ensure thread-safety.
    /// </summary>
    private void Synchronize(Action action, bool check = true)
    {
        _semaphore.Wait();
        try
        {
            if (check && HasBeenPublished)
                throw new InvalidOperationException("The event publisher has already been published; it cannot be reused by default. Use Reset() to continue using the publisher.");

            action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public void Clear() => Synchronize(_queue.Clear);

    /// <inheritdoc/>
    public void Reset() => Synchronize(() =>
    {
        _queue.Clear();
        HasBeenPublished = false;
    }, false);

    /// <inheritdoc/>
    public void Rollback(int count) => Synchronize(() =>
    {
        count.ThrowWhen(count => count > _queue.Count, $"A {nameof(Rollback)} count cannot exceed the current queue length/count.");

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                _queue.RemoveLast();
            }
        }
    });

    /// <inheritdoc/>
    /// <remarks>This will also <see cref="IEventFormatter.AddTracing"/> prior to the underlying <see cref="OnPublishAsync(DestinationEvent[], CancellationToken)"/>.</remarks>
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (HasBeenPublished)
                throw new InvalidOperationException("The event publisher has already been published; it cannot be reused by default. Use Reset() to continue using the publisher.");

            if (_queue.Count == 0)
            {
                HasBeenPublished = true;
                return;
            }

            // We have something to publish, so do it.
            await Invoker.InvokeAsync(this, async (tracer, cancellationToken) =>
            {
                var events = _queue.ToArray();
                foreach (var de in events)
                    Formatter.AddTracing(de.Event);

                if (Logger?.IsEnabled(LogLevel.Debug) ?? false)
                {
                    var list = _queue.Select(kvp => new { destination = kvp.Destination, @event = kvp.Event.EncodeToJsonElement() });
                    Logger.LogDebug("Preparing to send {Length} event(s):{NewLine}{Json}", events.Length, Environment.NewLine, JsonSerializer.Serialize(list, _debugJsonSerializerOptions ??= new JsonSerializerOptions { WriteIndented = true }));
                }

                await OnPublishAsync(events, cancellationToken).ConfigureAwait(false);

                HasBeenPublished = true;
                tracer.Activity?.AddTag("event.published.count", events.Length);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Publishes (sends) all previously added (queued) <paramref name="events"/> to the underlying eventing/persistence subsystem.
    /// </summary>
    /// <param name="events">One or more <see cref="DestinationEvent"/> objects.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>All <see cref="IEventFormatter.Format(EventData)"/>, <see cref="IEventFormatter.ConvertToCloudEvent(EventData)"/> and <see cref="IEventFormatter.AddTracing"/> operations have been performed prior.
    /// <para>Note: the <see cref="OnPublishAsync(DestinationEvent[], CancellationToken)"/> will only be called where there is at least a single event to be published; i.e. the <paramref name="events"/> will never be empty.</para></remarks>
    protected abstract Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public DestinationEvent[] GetEvents() => [.. _queue];
}