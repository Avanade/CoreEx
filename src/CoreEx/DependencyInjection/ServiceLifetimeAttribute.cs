namespace CoreEx.DependencyInjection;

/// <summary>
/// Provides the base <see cref="IServiceCollection"/> <see cref="ServiceLifetime"/> auto-registering capability with optional <see cref="Key"/>.
/// </summary>
/// <param name="ServiceType">The service <see cref="Type"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class ServiceLifetimeAttribute(Type? ServiceType = null) : Attribute
{
    /// <summary>
    /// Gets the <see cref="ServiceLifetime"/>.
    /// </summary>
    public abstract ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets or sets the key of the service.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the service <see cref="Type"/>.
    /// </summary>
    public Type? ServiceType { get; set; } = ServiceType;

    /// <summary>
    /// Adds the <paramref name="implementationType"/> to the specified <see cref="IServiceCollection"/> using the underlying configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="implementationType">The implementation <see cref="Type"/>.</param>
    public void AddService(IServiceCollection services, Type implementationType)
    {
        services.ThrowIfNull();
        implementationType.ThrowIfNull();

        if (ServiceType is not null && !ServiceType.IsAssignableFrom(implementationType))
            throw new InvalidOperationException($"The service type '{ServiceType}' is not assignable from the implementation type '{implementationType}'.");

        services.TryAdd(new ServiceDescriptor(ServiceType ?? implementationType, Key, implementationType, Lifetime));
    }

    /// <summary>
    /// Adds the <paramref name="implementationType"/> to the specified <see cref="IServiceCollection"/> using the underlying configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="implementationType">The implementation <see cref="Type"/>.</param>
    /// <param name="factory">The factory method to create the service.</param>
    public void AddService(IServiceCollection services, Type implementationType, Func<IServiceProvider, object?, object> factory)
    {
        services.ThrowIfNull();
        implementationType.ThrowIfNull();
        if (ServiceType is not null && !ServiceType.IsAssignableFrom(implementationType))
            throw new InvalidOperationException($"The service type '{ServiceType}' is not assignable from the implementation type '{implementationType}'.");

        services.TryAdd(new ServiceDescriptor(ServiceType ?? implementationType, Key, factory.ThrowIfNull(), Lifetime));
    }

    /// <summary>
    /// Gets the <see cref="ServiceLifetimeAttribute"/> for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the <see cref="ServiceLifetimeAttribute"/> for.</param>
    /// <returns>The <see cref="ServiceLifetimeAttribute"/> where found; otherwise, <see langword="null"/>.</returns>
    public static ServiceLifetimeAttribute? GetCustomAttribute(Type type) => type.GetCustomAttributes(typeof(ServiceLifetimeAttribute), true).SingleOrDefault() as ServiceLifetimeAttribute;
}