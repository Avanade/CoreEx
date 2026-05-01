namespace CoreEx.UnitTesting.Events;

/// <summary>
/// Provides <see cref="IEventPublisher"/>-specific expectations for unit testing.
/// </summary>
/// <typeparam name="TTester">The tester <see cref="Type"/>.</typeparam>
public class EventExpectations<TTester> : ExpectationsBase<TTester>
{
    private readonly Dictionary<string, EventExpectationsConfig> _expectations = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventExpectations{TTester}"/> class.
    /// </summary>
    /// <param name="owner">The owning <see cref="TesterBase"/>.</param>
    /// <param name="tester">The initiating tester.</param>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="assembly">The assembly to use for resource resolution.</param>
    internal EventExpectations(TesterBase owner, TTester tester, string? requestId, Assembly assembly) : base(owner, tester)
    {
        RequestId = requestId;
        ResourceAssembly = assembly;
    }

    /// <inheritdoc/>
    public override string Title => "Event expectations";

    /// <inheritdoc/>
    /// <remarks>Overrides to ensure it occurs after majority.</remarks>
    public override int Order => 5000;

    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    internal string? RequestId { get; }

    /// <summary>
    /// Gets the assembly to use for resource resolution.
    /// </summary>
    internal Assembly ResourceAssembly { get; }

    /// <summary>
    /// Expects that no events will have been published for the keyed <see cref="IEventPublisher"/>.
    /// </summary>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public void ExpectNoEvents(string serviceKey)
    {
        GetOrAddConfig(serviceKey).ExpectNoEvents();
    }

    /// <summary>
    /// Expects that events will have been published for the keyed <see cref="IEventPublisher"/>.
    /// </summary>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <param name="configure">The action to enable events expectations configuration.</param>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public void ExpectEvents(string serviceKey, Action<EventExpectationsConfig>? configure = null)
    {
        var config = GetOrAddConfig(serviceKey);
        config.ExpectEvents();
        configure?.Invoke(config);
    }

    /// <summary>
    /// Gets or adds the expectation configuration for the specified service key.
    /// </summary>
    private EventExpectationsConfig GetOrAddConfig(string serviceKey)
    {
        if (!_expectations.TryGetValue(serviceKey.ThrowIfNullOrEmpty(), out var config))
        {
            config = new EventExpectationsConfig(Owner, serviceKey, RequestId, ResourceAssembly);
            _expectations[serviceKey] = config;
        }

        return config;
    }

    /// <inheritdoc/>
    protected override Task OnAssertAsync(AssertArgs args)
    {
        foreach (var kvp in _expectations)
        {
            kvp.Value.Assert(args);
        }

        return Task.CompletedTask;
    }
}