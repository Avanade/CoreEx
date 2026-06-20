namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides <see cref="EventSubscriberBase"/> subscription and execution management.
/// </summary>
/// <remarks>Note: <see cref="IEventSubscriberInbox.InboxCheckAsync"/> is enabled using one of the <see cref="RequiresInbox(bool)"/> methods to set up.</remarks>
/// <param name="invoker">The optional <see cref="SubscribedInvoker"/>.</param>
public sealed class SubscribedManager(SubscribedInvoker? invoker = null)
{
    private readonly SubscribedInvoker _invoker = invoker ?? new();
    private readonly List<(Type Type, SubscribeAttribute[] Attributes)> _subscribers = [];
    private IEventSubscriberInbox? _inbox;
    private Type? _inboxType;

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> for when an <see cref="EventData"/> has no configured subscriber.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.CompleteAsSilent"/>.</remarks>
    public ErrorHandling NotSubscribedHandling { get; set; } = ErrorHandling.CompleteAsSilent;

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> for when an <see cref="EventData"/> has multiple subscribers.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.Catastrophic"/>.</remarks> 
    public ErrorHandling AmbiguousSubscriberHandling { get; set; } = ErrorHandling.Catastrophic;

    /// <summary>
    /// Indicates whether all subscribers require an <b>inbox</b> check (unless explicitly overridden) before processing.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. This is set using <see cref="RequiresInbox(bool)"/>.
    /// <para>An individual subscriber can override by setting its <see cref="SubscribedBase.RequiresInboxCheck"/> value.</para></remarks>
    public bool RequiresInboxCheck { get; private set; } = false;

    /// <summary>
    /// Sets the <b>inbox</b> to the specified <paramref name="inbox"/> and <paramref name="requiresInboxCheck"/> to enable inbox checking.
    /// </summary>
    /// <param name="inbox">The <see cref="IEventSubscriberInbox"/>.</param>
    /// <param name="requiresInboxCheck">Indicates whether all subscribers require an <paramref name="inbox"/> check (unless explicitly overridden) before processing.</param>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    public SubscribedManager RequiresInbox(IEventSubscriberInbox inbox, bool requiresInboxCheck = true)
    {
        _inbox = inbox.ThrowIfNull();
        _inboxType = null;
        RequiresInboxCheck = requiresInboxCheck;
        return this;
    }

    /// <summary>
    /// Sets the <b>inbox</b> to the specified <typeparamref name="TInbox"/> and <paramref name="requiresInboxCheck"/> to enable inbox checking.
    /// </summary>
    /// <typeparam name="TInbox">The <see cref="IEventSubscriberInbox"/> <see cref="Type"/>.</typeparam>
    /// <param name="requiresInboxCheck">Indicates whether all subscribers require an <b>inbox</b> check (unless explicitly overridden) before processing.</param>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <typeparamref name="TInbox"/> will be instantiated using dependency injection per <see cref="ReceiveAsync(ExecutionContext, SubscribedBase, EventData, EventSubscriberArgs, CancellationToken)"/> execution (as needed).</remarks>
    public SubscribedManager RequiresInbox<TInbox>(bool requiresInboxCheck = true) where TInbox : IEventSubscriberInbox
    {
        _inboxType = typeof(TInbox);
        _inbox = null;
        RequiresInboxCheck = requiresInboxCheck;
        return this;
    }

    /// <summary>
    /// Sets the <b>inbox</b> to the DI configured <see cref="IEventSubscriberInbox"/> and <paramref name="requiresInboxCheck"/> to enable inbox checking.
    /// </summary>
    /// <param name="requiresInboxCheck">Indicates whether all subscribers require an <b>inbox</b> check (unless explicitly overridden) before processing.</param>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <see cref="IEventSubscriberInbox"/> will be instantiated using dependency injection per <see cref="ReceiveAsync(ExecutionContext, SubscribedBase, EventData, EventSubscriberArgs, CancellationToken)"/> execution (as needed).</remarks>
    public SubscribedManager RequiresInbox(bool requiresInboxCheck = true) => RequiresInbox<IEventSubscriberInbox>(requiresInboxCheck);

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> for when the <see cref="EventData"/> fails the <see cref="IEventSubscriberInbox.InboxCheckAsync"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="ErrorHandling.CompleteAsWarning"/> as this is an expected byproduct of achieving at least once only idempotent processing; i.e. event/message has previously been processed
    /// (either successfully or unsuccessfully) and further attempts should be ignored.</remarks>
    public ErrorHandling InboxFailureHandling { get; set; } = ErrorHandling.CompleteAsWarning;

    /// <summary>
    /// Adds a single subscriber type.
    /// </summary>
    /// <typeparam name="T">The subscriber type to add.</typeparam>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    public SubscribedManager AddSubscriber<T>() where T : SubscribedBase => AddSubscribers(typeof(T));

    /// <summary>
    /// Adds multiple subscriber types.
    /// </summary>
    /// <param name="types">The subscriber types to add.</param>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Each type must implement both <see cref="SubscribedBase"/> and at least be decorated with one <see cref="SubscribeAttribute"/> otherwise an <see cref="ArgumentException"/> will be thrown.</remarks>
    public SubscribedManager AddSubscribers(params IEnumerable<Type> types)
    {
        foreach (var type in types.Distinct().Where(t => !_subscribers.Any(x => x.Type == t)))
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericType)
                throw new ArgumentException($"Type {type.Name} is not a class, is abstract and/or generic and as such cannot be used as a subscriber.", nameof(types));

            if (!typeof(SubscribedBase).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.Name} does not inherit from {nameof(SubscribedBase)}.", nameof(types));

            var atts = type.GetCustomAttributes<SubscribeAttribute>().ToArray();
            if (atts.Length == 0)
                throw new ArgumentException($"Type {type.Name} is not decorated with any {nameof(SubscribeAttribute)} attributes and as such cannot be used as a subscriber.", nameof(types));

            _subscribers.Add((type, atts));
        }

        return this;
    }

    /// <summary>
    /// Dynamically adds all subscribers within the specified assembly that implement <see cref="SubscribedBase"/> and are decorated with at least one <see cref="SubscribeAttribute"/>.
    /// </summary>
    /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <returns>The <see cref="SubscribedManager"/> to support fluent-style method-chaining.</returns>
    public SubscribedManager AddSubscribersUsing<TAssembly1>()
    {
        var assembly = typeof(TAssembly1).Assembly;
        var types = assembly.GetTypes().Where(t => typeof(SubscribedBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && !t.IsGenericType && t.GetCustomAttributes<SubscribeAttribute>().Any());
        return AddSubscribers([.. types]);
    }

    /// <summary>
    /// Matches and creates an instance of the <see cref="SubscribedBase"/> where found; otherwise, an error will occur.
    /// </summary>
    /// <param name="executionContext">The current <see cref="ExecutionContext"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="title">The event title (i.e. <see cref="EventData.Title"/>) to match.</param>
    /// <param name="source">The event source <see cref="Uri"/> (i.e. <see cref="EventData.Source"/> to match.</param>
    /// <returns>The <see cref="Result{T}"/> with the instantiated <see cref="SubscribedBase"/>; otherwise, the corresponding <see cref="Result.Fail(Exception)"/>.
    /// <remarks>The <see cref="EventSubscriberArgs.UsesSubscribedManager"/> property is set to <see langword="true"/> and the <see cref="EventSubscriberArgs.Subscriber"/> property is set to the matched
    /// <see cref="SubscribedBase"/> instance (where matched).</remarks></returns>
    public Result<SubscribedBase> Match(ExecutionContext executionContext, EventSubscriberArgs args, string? title, Uri? source = null)
    {
        executionContext.ThrowIfNull();
        args.ThrowIfNull();
        args.Owner.ThrowIfNull();
        args.UsesSubscribedManager = true;

        var subscribers = _subscribers
            .Where(x => x.Attributes.Any(a => a.IsMatch(title, source)))
            .Select(x => x.Type)
            .ToArray();

        // Handle where no subscriber is found.
        if (subscribers.Length == 0)
        {
            var eha = new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), ErrorHandlingOverride = NotSubscribedHandling, Exception = new InvalidOperationException("No subscriber matched the event.") };
            return args.Owner.ErrorHandler.Handle(eha, defaultErrorHandling: null);
        }

        // Handle where more than one subscriber is found.
        if (subscribers.Length > 1)
        {
            var eha = new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), ErrorHandlingOverride = AmbiguousSubscriberHandling, Exception = new InvalidOperationException($"Multiple subscribers ({subscribers.Length}) matched the event.") };
            return args.Owner.ErrorHandler.Handle(eha, defaultErrorHandling: null);
        }

        // Instantiate the matched subscriber; failure is always considered 'Catastrophic'.
        try
        {
            return args.Subscriber = (SubscribedBase)executionContext.ServiceProvider.ThrowIfNull().GetRequiredService(subscribers[0]);
        }
        catch (Exception ex)
        {
            return args.Owner.ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), Exception = new InvalidOperationException($"Unable to instantiate matched Subscribed type '{subscribers[0].Name}': {ex.Message}"), ErrorHandlingOverride = ErrorHandling.Catastrophic }, null);
        }
    }

    /// <summary>
    /// Receives and processes the <see cref="EventData"/> using the <paramref name="subscribed"/> instance.
    /// </summary>
    /// <param name="executionContext">The current <see cref="ExecutionContext"/>.</param>
    /// <param name="subscribed">The previously matched <see cref="SubscribedBase"/>.</param>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>It is expected that a <see cref="Match(ExecutionContext, EventSubscriberArgs, string?, Uri?)"/> is performed prior to match and instantiate the required <paramref name="subscribed"/> instance.</remarks>
    public async Task<Result> ReceiveAsync(ExecutionContext executionContext, SubscribedBase subscribed, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
    {
        executionContext.ThrowIfNull();
        executionContext.ServiceProvider.ThrowIfNull();
        subscribed.ThrowIfNull();
        @event.ThrowIfNull();
        args.Owner.ThrowIfNull();

        // Where the subscriber requires an inbox check then action accordingly.
        if (subscribed.RequiresInboxCheck ?? RequiresInboxCheck)
        {
            if (_inbox is null && _inboxType is null)
                throw new InvalidOperationException($"The Inbox is not configured and the subscriber requires an inbox check; use {nameof(RequiresInbox)} to configure.");

            var inbox = _inbox ?? (IEventSubscriberInbox)executionContext.ServiceProvider.GetRequiredService(_inboxType!);

            if (!await inbox.InboxCheckAsync(@event, args, cancellationToken).ConfigureAwait(false))
            {
                var eha = new ErrorHandlerArgs { SubscriberArgs = args, SourceType = GetType(), ErrorHandlingOverride = InboxFailureHandling, Exception = new InvalidOperationException("The event failed the inbox check and will not be processed.") };
                return args.Owner.ErrorHandler.Handle(eha, defaultErrorHandling: null);
            }
        }

        // Execute the subscribed receive.
        return await _invoker.InvokeAsync(subscribed, async (_, cancellationToken) =>
        {
            try
            {
                return Result.Go(await subscribed.ReceiveAsync(@event, args, cancellationToken).ConfigureAwait(false))
                    .OnFailure(result => subscribed.ErrorHandler is not null ? subscribed.ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = subscribed.GetType(), Exception = result.Error }, null) : result);
            }
            catch (Exception ex) when (subscribed.ErrorHandler is not null && ex is not IEventSubscriberException && !ex.IsCanceled()) // Ignore IEventSubscriberException's and *CanceledException as they are intended to bubble up!
            {
                return subscribed.ErrorHandler.Handle(new ErrorHandlerArgs { SubscriberArgs = args, SourceType = subscribed.GetType(), Exception = ex }, null);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}