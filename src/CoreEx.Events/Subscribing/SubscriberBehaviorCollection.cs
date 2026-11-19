namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the ordered <see cref="ISubscriberBehavior"/> registrations for an <see cref="EventSubscriberBase"/>.
/// </summary>
/// <remarks>Behaviors run outermost-first in the order added. Type registrations are resolved once per delivery from the current <see cref="ExecutionContext.ServiceProvider"/> (registered service or
/// constructor-activated); instance registrations are shared across deliveries and must be thread-safe.</remarks>
public sealed class SubscriberBehaviorCollection
{
    private readonly List<(Type? Type, ISubscriberBehavior? Instance)> _entries = [];

    /// <summary>
    /// Gets the number of registered behaviors.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds a behavior type resolved per delivery.
    /// </summary>
    /// <typeparam name="TBehavior">The <see cref="ISubscriberBehavior"/> type.</typeparam>
    /// <returns>The <see cref="SubscriberBehaviorCollection"/> to support fluent-style method-chaining.</returns>
    public SubscriberBehaviorCollection Add<TBehavior>() where TBehavior : class, ISubscriberBehavior
    {
        _entries.Add((typeof(TBehavior), null));
        return this;
    }

    /// <summary>
    /// Adds a shared behavior instance.
    /// </summary>
    /// <param name="behavior">The <see cref="ISubscriberBehavior"/>.</param>
    /// <returns>The <see cref="SubscriberBehaviorCollection"/> to support fluent-style method-chaining.</returns>
    public SubscriberBehaviorCollection Add(ISubscriberBehavior behavior)
    {
        _entries.Add((null, behavior.ThrowIfNull()));
        return this;
    }

    /// <summary>
    /// Resolves the behavior instances for a delivery.
    /// </summary>
    internal ISubscriberBehavior[] Resolve()
    {
        var behaviors = new ISubscriberBehavior[_entries.Count];
        IServiceProvider? serviceProvider = null;

        for (var i = 0; i < behaviors.Length; i++)
        {
            var (type, instance) = _entries[i];
            if (instance is not null)
            {
                behaviors[i] = instance;
                continue;
            }

            serviceProvider ??= ExecutionContext.TryGetCurrent(out var ec) ? ec.ServiceProvider : null;
            if (serviceProvider is null)
                throw new InvalidOperationException($"Subscriber behavior '{type!.Name}' requires an ExecutionContext with a ServiceProvider to be resolved.");

            behaviors[i] = (ISubscriberBehavior)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type!);
        }

        return behaviors;
    }
}
