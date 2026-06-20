namespace CoreEx.UnitTesting.Events;

/// <summary>
/// Provides the <see cref="EventExpectations{TTester}"/> configuration for a specific <see cref="IEventPublisher"/> service key, enabling the configuration of expected events and their assertion during testing.
/// </summary>
/// <remarks>Where expected events have been specified they will be matched, in the sequence specified, against the actual events published during the test execution.</remarks>
public sealed class EventExpectationsConfig
{
    private bool _expectNoEvents;
    private readonly List<EventExpectationAssertor> _assertors = [];
    private readonly List<string> _pathsToIgnore = [.. DefaultPathsToIgnore];
    private Action<AssertArgs, DestinationEvent[]>? _assertAllEvents;

    /// <summary>
    /// Gets or sets the default <see cref="CloudEvent"/> metadata paths to ignore for comparisons.
    /// </summary>
    public static List<string> DefaultMetadataPathsToIgnore { get; set => field = value.ThrowIfNull(); } = ["id", "time", "subject", "partitionkey", "authtype", "authid", "traceparent", "tracestate", "baggage"];

    /// <summary>
    /// Gets or sets the default <see cref="CloudEvent"/> data paths to ignore for comparisons.
    /// </summary>
    public static List<string> DefaultDataPathsToIgnore { get; set => field = value.ThrowIfNull(); } = ["data.id", "data.changelog", "data.etag"];

    /// <summary>
    /// Gets the default JSON <see cref="CloudEvent"/> paths to ignore for comparisons, which is a combination of the <see cref="DefaultMetadataPathsToIgnore"/> and <see cref="DefaultDataPathsToIgnore"/>.
    /// </summary>
    public static List<string> DefaultPathsToIgnore => [.. DefaultMetadataPathsToIgnore, .. DefaultDataPathsToIgnore];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventExpectationsConfig"/> class.
    /// </summary>
    /// <param name="tester">The owning <see cref="TesterBase"/>.</param>
    /// <param name="serviceKey">The registered service key.</param>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="assembly">The assembly to use for resource resolution.</param>
    internal EventExpectationsConfig(TesterBase tester, string serviceKey, string? requestId, Assembly assembly)
    {
        Tester = tester;
        ServiceKey = serviceKey;
        RequestId = requestId;
        ResourceAssembly = assembly;
    }

    /// <summary>
    /// Gets the owning <see cref="TesterBase"/>.
    /// </summary>
    internal TesterBase Tester { get; }

    /// <summary>
    /// Gets the registered service key for the underlying <see cref="IEventPublisher"/>.
    /// </summary>
    internal string ServiceKey { get; }

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    internal string? RequestId { get; }

    /// <summary>
    /// Gets the assembly to use for resource resolution.
    /// </summary>
    internal Assembly ResourceAssembly { get; }

    /// <summary>
    /// Gets the current list of JSON paths to ignore for all event comparisons.
    /// </summary>
    /// <remarks><para>Defaults to <see cref="DefaultPathsToIgnore"/>.</para>
    /// Note: this list is copied and concatenated with the path(s) specified for the individual event expectations. Therefore, any changes after an event expectation is made, will not be included; i.e. only
    /// applies to subsequent event expectations. This is to ensure consistency for the individual event expectation configurations, and to avoid any unintended consequences of changes to the default paths
    /// after preceding event expectations have been configured.</remarks>
    public List<string> PathsToIgnore => _pathsToIgnore;

    /// <summary>
    /// Indicates that no events should have been published.
    /// </summary>
    internal void ExpectNoEvents()
    {
        if (_assertors.Count != 0)
            throw new InvalidOperationException("Cannot set to expect no events when event expectations have already been configured.");

        _expectNoEvents = true;
    }

    /// <summary>
    /// Indicates that events should have been published.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void ExpectEvents()
    {
        if (_expectNoEvents)
            throw new InvalidOperationException("Cannot set to expect events when already configured to expect no events.");

        if (_assertAllEvents is not null)
            throw new InvalidOperationException($"Cannot set to expect events when already configured using {nameof(AssertAllFromJsonResource)}, {nameof(AssertCount)}, or {nameof(Assert)}.");
    }

    /// <summary>
    /// Gets or sets the factory function used to create new <see cref="ExecutionContext"/> instance.
    /// </summary>
    public Func<IServiceProvider, ExecutionContext> ExecutionContextFactory { get; set; } = sp => new ExecutionContext();

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/>; where <see langword="null"/> then the registered version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IHostSettings"/>; where <see langword="null"/> then the registered host version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public IHostSettings? HostSettings { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IEventFormatter"/>; where <see langword="null"/> then the registered version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public IEventFormatter? EventFormatter { get; set; }

    /// <summary>
    /// Adds an expectation assertion that the consumer of the <paramref name="events"/> action is responsible for asserting as required.
    /// </summary>
    /// <param name="events">The actual <see cref="DestinationEvent"/> array.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig Assert(Action<DestinationEvent[]> events)
    {
        ExpectEvents();
        events.ThrowIfNull();

        _assertAllEvents = (_, actual) =>
        {
            events(actual);
            Tester.Implementor.WriteLine($"    > Expected zero or more event(s) with a custom Assert; and that assertion was met.");
        };

        return this;
    }

    /// <summary>
    /// Adds am expectation assertion that asserts that the number of published events matches the specified <paramref name="count"/>..
    /// </summary>
    /// <param name="count">The expected number of events to be published.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCount(int count)
    {
        ExpectEvents();
        count.ThrowIfLessThanOrEqualToZero();

        _assertAllEvents = (_, actual) =>
        {
            if (count == actual.Length)
                Tester.Implementor.WriteLine($"    > Expected {count} event(s); and that number was found to be published.");
            else
                Tester.Implementor.AssertFail($"Expected {_assertors.Count} '{ServiceKey}' events; however, {actual.Length} were found to be published.");
        };

        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that one or more events were published matching the <see cref="DestinationEvent"/> array from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/> for the embedded resource.</typeparam>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="pathsToIgnore"/> should be from an individual <see cref="CloudEvent"/> perspective from a consistency perspective.</remarks>
    public EventExpectationsConfig AssertAllFromJsonResource<TAssembly>(string resourceName, params IEnumerable<string> pathsToIgnore)
        => AssertAllFromJsonResource(resourceName, typeof(TAssembly).Assembly, pathsToIgnore);

    /// <summary>
    /// Adds an expectation assertion that one or more events were published matching the <see cref="DestinationEvent"/> array from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="pathsToIgnore"/> should be from an individual <see cref="CloudEvent"/> perspective from a consistency perspective.</remarks>
    public EventExpectationsConfig AssertAllFromJsonResource(string resourceName, params IEnumerable<string> pathsToIgnore)
        => AssertAllFromJsonResource(resourceName, null, pathsToIgnore);

    /// <summary>
    /// Adds an expectation assertion that one or more events were published matching the <see cref="DestinationEvent"/> array from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="assembly">The <see cref="ResourceAssembly"/> that contains the resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="pathsToIgnore"/> should be from an individual <see cref="CloudEvent"/> perspective from a consistency perspective.</remarks>
    public EventExpectationsConfig AssertAllFromJsonResource(string resourceName, Assembly? assembly = null, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();

        assembly ??= ResourceAssembly;
        var capturedPaths = CombinePaths(pathsToIgnore).Select(x => $"event.{(x.StartsWith("$.") ? x[2..] : x)}").ToArray();
        _assertAllEvents = (args, events) =>
        {
            var ej = Resource.GetJson(resourceName, assembly);
            var aj = JsonSerializer.Serialize(events.Select(kvp => new { destination = kvp.Destination, @event = kvp.Event.EncodeToJsonElement() }));

            ObjectComparer.AssertJson(new UnitTestEx.Json.JsonElementComparerOptions { PreambleText = $"'{ServiceKey}' event(s) comparison." }, ej, aj, capturedPaths);
            Tester.Implementor.WriteLine("    > All event expectations met successfully.");
        };

        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published where the returned value will be used as the source of the <see cref="EventData.Data"/>.
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="title">The expected <see cref="EventData.Title"/> (.</param>
    /// <param name="messageType">The expected <see cref="EventData.MessageType"/>.</param>
    /// <param name="updater">An optional action to further update the expected <see cref="EventData"/> prior to assertion.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Internally this constructs the <see cref="EventData"/> and converts it to a <see cref="CloudEvent"/> for comparison.</remarks>
    public EventExpectationsConfig AssertWithValue(string destination, string title, CoreEx.Events.MessageType messageType = CoreEx.Events.MessageType.Event, Action<EventData>? updater = null, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();

        _assertors.Add(new EventExpectationAssertor(this, destination, (assertor, args, _) =>
        {
            var ed = new EventData() { Title = title, MessageType = messageType }.WithValue(args.Value, null, assertor.JsonSerializerOptions);
            updater?.Invoke(ed);
            return assertor.EventFormatter.ConvertToCloudEvent(assertor.EventFormatter.Format(ed));
        }, CombinePaths(pathsToIgnore)));

        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the specified <see cref="EventData"/> converted as a <see cref="CloudEvent"/> (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="eventData">The <see cref="EventData"/>.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertEventData(string destination, EventData eventData, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();
        eventData.ThrowIfNull();

        _assertors.Add(new EventExpectationAssertor(this, destination, (assertor, args, _) =>
        {
            return assertor.EventFormatter.ConvertToCloudEvent(assertor.EventFormatter.Format(eventData));
        }, CombinePaths(pathsToIgnore)));
        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the primary metadata (where specified) only.
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="title">The expected <see cref="EventData.Title"/> (<see cref="CloudEvent.Type"/>) glob-like matching pattern that will represent the underlying title <see cref="Regex"/>.</param>
    /// <param name="key">The expected <see cref="EventData.Key"/> (<see cref="CloudEvent.Subject"/>) value.</param>
    /// <param name="source">The expected <see cref="EventData.Source"/> (<see cref="CloudEvent.Source"/>) glob-like matching pattern that will represent the underlying source <see cref="Regex"/>.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="title"/> and <paramref name="source"/> both support glob-like matching patterns.</remarks>
    public EventExpectationsConfig AssertMetadata(string destination, string? title = null, string? key = null, string? source = null)
    {
        ExpectEvents();

        _assertors.Add(new EventExpectationAssertor(this, destination, (assertor, args, actual) =>
        {
            assertor.AssertDestination(actual.Destination);

            var sa = new SubscribeAttribute(title, source);
            if (sa.IsMatch(actual.Event?.Type, actual.Event?.Source))
            {
                if (key != null)
                    assertor.Tester.Implementor.AssertAreEqual(key, actual.Event?.Subject, $"Expected key '{key}'; but found '{actual.Event?.Subject}'.");
            }
            else
                assertor.Tester.Implementor.AssertFail($"Expected event metadata did not match; Title expected '{title}' vs actual '{actual.Event?.Type}', Source expected '{source}' vs actual '{actual.Event?.Source}'");
        }, []));

        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the specified <see cref="CloudEvent"/> (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="cloudEvent">The expected <see cref="CloudEvent"/>. </param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCloudEvent(string destination, CloudEvent cloudEvent, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();
        _assertors.Add(new EventExpectationAssertor(this, destination, (_, _, _) => cloudEvent, pathsToIgnore));
        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the <see cref="CloudEvent"/> from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/> for the embedded resource.</typeparam>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCloudEventFromJsonResource<TAssembly>(string destination, string resourceName, params IEnumerable<string> pathsToIgnore)
        => AssertCloudEventFromJsonResource<TAssembly>(destination, resourceName, null, pathsToIgnore);

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the <see cref="CloudEvent"/> from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the <see cref="Assembly"/> for the embedded resource.</typeparam>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="updater">An optional action to update the <see cref="CloudEvent"/> before use.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCloudEventFromJsonResource<TAssembly>(string destination, string resourceName, Action<CloudEvent>? updater = null, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();

        _assertors.Add(new EventExpectationAssertor(this, destination, (assertor, args, _) =>
        {
            return assertor.Tester.CreateCloudEventFromJsonResource<TAssembly>(resourceName, updater);
        }, CombinePaths(pathsToIgnore)));

        return this;
    }

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the <see cref="CloudEvent"/> from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCloudEventFromJsonResource(string destination, string resourceName, params IEnumerable<string> pathsToIgnore)
        => AssertCloudEventFromJsonResource(destination, resourceName, null, null, pathsToIgnore);

    /// <summary>
    /// Adds an expectation assertion that an event was published matching the <see cref="CloudEvent"/> from the specified JSON resource (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="resourceName">The embedded resource name (matches to the end of the fully qualified resource name).</param>
    /// <param name="updater">An optional action to update the <see cref="CloudEvent"/> before use.</param>
    /// <param name="assembly">The <see cref="ResourceAssembly"/> that contains the resource; defaults to <see cref="Assembly.GetCallingAssembly"/>.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    public EventExpectationsConfig AssertCloudEventFromJsonResource(string destination, string resourceName, Action<CloudEvent>? updater = null, Assembly? assembly = null, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();

        _assertors.Add(new EventExpectationAssertor(this, destination, (assertor, args, _) =>
        {
            return assertor.Tester.CreateCloudEventFromJsonResource(resourceName, updater, assembly ?? ResourceAssembly);
        }, CombinePaths(pathsToIgnore)));

        return this;
    }

    /// <summary>
    /// Adds a custom expectation assertion that an event was published where the provided <paramref name="customAssert"/> will be invoked to perform the assertion against the actual <see cref="DestinationEvent"/> (with any specified JSON paths to ignore).
    /// </summary>
    /// <param name="destination">The expected destination (i.e. topic) name.</param>
    /// <param name="customAssert">The custom assert action.</param>
    /// <param name="pathsToIgnore">Any additional JSON paths to ignore from the underlying <see cref="CloudEvent"/> comparison.</param>
    /// <returns>The <see cref="EventExpectationsConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <see cref="EventExpectationAssertor.AssertDestination(string)"/> and <see cref="EventExpectationAssertor.AssertCloudEvent(CloudEvent, CloudEvent)"/> methods can be used within the custom assert action for consistency.</remarks>
    public EventExpectationsConfig AssertCustom(string destination, Action<EventExpectationAssertor, AssertArgs, DestinationEvent> customAssert, params IEnumerable<string> pathsToIgnore)
    {
        ExpectEvents();
        _assertors.Add(new EventExpectationAssertor(this, destination, customAssert, pathsToIgnore));
        return this;
    }

    /// <summary>
    /// Combines the specified array of paths to ignore with the existing set of ignored paths.
    /// </summary>
    private string[] CombinePaths(IEnumerable<string> pathsToIgnore) => _pathsToIgnore.Count == 0 ? [.. pathsToIgnore] : [.. _pathsToIgnore, .. pathsToIgnore];

    /// <summary>
    /// Performs the assertion of the expected events against those that were published, throwing an exception if any expectations were not met.
    /// </summary>
    /// <param name="args">The <see cref="AssertArgs"/>.</param>
    internal void Assert(AssertArgs args)
    {
        Tester.Implementor.WriteLine($"  > '{ServiceKey}' events.");

        // High-level checks.
        if (!Tester.SharedState.RequestStateData(RequestId).TryGetValue(ServiceKey, out var obj) || obj is not DestinationEvent[] events || events.Length == 0)
        {
            if (_expectNoEvents)
            {
                Tester.Implementor.WriteLine("    > Expected no events and there were none.");
                return;
            }

            args.Tester.Implementor.AssertFail($"Expected '{ServiceKey}' events; however, no events were found to be published.");
            return;
        }

        if (_expectNoEvents)
        {
            Tester.Implementor.AssertFail($"Expected no '{ServiceKey}'events; however, {events.Length} found to be published.");
            return;
        }

        if (_assertAllEvents is not null)
        {
            _assertAllEvents(args, events);
            return;
        }

        if (_assertors.Count == 0)
        {
            Tester.Implementor.WriteLine($"    > Expected events; and {events.Length} found to be published.");
            return;
        }

        if (_assertors.Count != events.Length)
        {
            Tester.Implementor.AssertFail($"Expected {_assertors.Count} '{ServiceKey}' events; however, {events.Length} were found to be published.");
            return;
        }

        // Iterate and check each.
        var index = 0;
        foreach (var assertor in _assertors)
        {
            Tester.Implementor.WriteLine($"    > Event {index + 1} of {_assertors.Count}.");
            assertor.Assert(args, events[index]);
            index++;
        }

        Tester.Implementor.WriteLine("    > Event expectations met successfully.");
    }
}