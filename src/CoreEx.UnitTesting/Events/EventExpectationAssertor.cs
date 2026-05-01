namespace CoreEx.UnitTesting.Events;

/// <summary>
/// Provides assertion capabilities for an expected event.
/// </summary>
public sealed class EventExpectationAssertor
{
    private readonly EventExpectationsConfig _config;
    private readonly string? _expectedDestination;
    private readonly string[] _pathsToIgnore;
    private readonly Func<EventExpectationAssertor, AssertArgs, DestinationEvent, CloudEvent>? _expectedEventFactory;
    private readonly Action<EventExpectationAssertor, AssertArgs, DestinationEvent>? _customAssert;

    private JsonSerializerOptions? _jsonSerializerOptions;
    private IHostSettings? _hostSettings;
    private IEventFormatter? _eventFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventExpectationAssertor"/> class with an <paramref name="expectedEventFactory"/>.
    /// </summary>
    /// <param name="config">The owning <see cref="EventExpectationsConfig"/>.</param>
    /// <param name="expectedDestination">The expected destination.</param>
    /// <param name="expectedEventFactory">The function to create the expected <see cref="CloudEvent"/>.</param>
    /// <param name="pathsToIgnore">The JSON paths to ignore.</param>
    internal EventExpectationAssertor(EventExpectationsConfig config, string? expectedDestination, Func<EventExpectationAssertor, AssertArgs, DestinationEvent, CloudEvent> expectedEventFactory, IEnumerable<string> pathsToIgnore)
    {
        _config = config;
        _expectedDestination = expectedDestination;
        _expectedEventFactory = expectedEventFactory;
        _pathsToIgnore = [.. pathsToIgnore];

        // Copy out key attributes from the config for use in the assertion - need the now version.
        _jsonSerializerOptions = _config.JsonSerializerOptions;
        _hostSettings = _config.HostSettings;
        _eventFormatter = _config.EventFormatter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventExpectationAssertor"/> class with a <paramref name="customAssert"/>.
    /// </summary>
    /// <param name="config">The owning <see cref="EventExpectationsConfig"/>.</param>
    /// <param name="expectedDestination">The expected destination.</param>
    /// <param name="customAssert">The custom assert action.</param>
    /// <param name="pathsToIgnore">The JSON paths to ignore.</param>
    internal EventExpectationAssertor(EventExpectationsConfig config, string? expectedDestination, Action<EventExpectationAssertor, AssertArgs, DestinationEvent> customAssert, IEnumerable<string> pathsToIgnore)
    {
        _config = config;
        _expectedDestination = expectedDestination;
        _customAssert = customAssert;
        _pathsToIgnore = [.. pathsToIgnore];

        // Copy out key attributes from the config for use in the assertion - need the now version.
        _jsonSerializerOptions = _config.JsonSerializerOptions;
        _hostSettings = _config.HostSettings;
        _eventFormatter = _config.EventFormatter;
    }

    /// <summary>
    /// Gets the owning <see cref="TesterBase"/>.
    /// </summary>
    public TesterBase Tester => _config.Tester;

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/>; where <see langword="null"/> then the registered version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions ??= _config.Tester.Services.GetService<JsonSerializerOptions>() ?? CoreEx.Json.JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets the <see cref="IHostSettings"/>; where <see langword="null"/> then the registered host version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public IHostSettings HostSettings => _hostSettings ??= _config.Tester.Services.GetService<IHostSettings>() ?? throw new InvalidOperationException($"A {nameof(IHostSettings)} instance is required to perform the event assertion; either set on the config or ensure it is registered in the services.");

    /// <summary>
    /// Gets the <see cref="IEventFormatter"/>; where <see langword="null"/> then the registered version from the <see cref="TesterBase.Services"/> will be used.
    /// </summary>
    public IEventFormatter EventFormatter => _eventFormatter ??= _config.Tester.Services.GetService<IEventFormatter>() ?? new EventFormatter(HostSettings);

    /// <summary>
    /// Asserts that the expected and actual <see cref="DestinationEvent"/> are equal, ignoring any specified paths.
    /// </summary>
    /// <param name="args">The <see cref="AssertArgs"/>.</param>
    /// <param name="actual">The actual <see cref="DestinationEvent"/>.</param>
    internal void Assert(AssertArgs args, DestinationEvent actual)
    {
        AssertDestination(actual.Destination);

        if (_expectedEventFactory is not null)
            AssertCloudEvent(_expectedEventFactory(this, args, actual), actual.Event);
        else
            _customAssert?.Invoke(this, args, actual);
    }

    /// <summary>
    /// Asserts that the previously configured expected destination and <paramref name="actual"/> destination are equal.
    /// </summary>
    /// <param name="actual">The actual destination.</param>
    public void AssertDestination(string actual) => Tester.Implementor.AssertAreEqual(_expectedDestination, actual, $"Expected '{_config.ServiceKey}' event destination '{_expectedDestination}'; but found '{actual}'.");

    /// <summary>
    /// Asserts that the expected and actual <see cref="CloudEvent"/> are equal, ignoring any previously configured JSON paths.
    /// </summary>
    /// <param name="expected">The expected <see cref="CloudEvent"/>.</param>
    /// <param name="actual">The actual <see cref="CloudEvent"/>.</param>
    public void AssertCloudEvent(CloudEvent expected, CloudEvent actual)
    {
        var ej = expected?.EncodeToJsonElement(JsonSerializerOptions) ?? null;
        var aj = actual?.EncodeToJsonElement(JsonSerializerOptions) ?? null;
        ObjectComparer.Assert(new UnitTestEx.Json.JsonElementComparerOptions { PreambleText = $"'{_config.ServiceKey}' event comparison." }, ej, aj, _pathsToIgnore);
    }
}