namespace CoreEx.DependencyInjection;

/// <summary>
/// Indicates that the underlying implementation <see cref="Type"/> should be registered as a <see cref="ServiceLifetime.Scoped"/> service with an optional <see cref="ServiceLifetimeAttribute.Key"/>.
/// </summary>
/// <typeparam name="TService">The service <see cref="Type"/>.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ScopedServiceAttribute<TService>() : ServiceLifetimeAttribute(typeof(TService))
{
    /// <inheritdoc/>
    public override ServiceLifetime Lifetime => ServiceLifetime.Scoped;
}